using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.Interop;
using ECommons.PartyFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Moodles.Data;
using Moodles.GameGuiProcessors;
using System.Buffers.Binary;
using System.Runtime.ConstrainedExecution;

namespace Moodles.Processors;
public unsafe class CommonProcessor : IDisposable
{
    public PartyListProcessor PartyListProcessor;
    public StatusCustomProcessor StatusCustomProcessor;
    public TargetInfoProcessor TargetInfoProcessor;
    public FocusTargetInfoProcessor FocusTargetInfoProcessor;
    public StatusProcessor StatusProcessor;
    public TargetInfoBuffDebuffProcessor TargetInfoBuffDebuffProcessor;
    public FlyPopupTextProcessor FlyPopupTextProcessor;
    public readonly List<string> StatusEffectPaths = ["Clear"];
    public readonly HashSet<uint> NegativeStatuses = [];
    public readonly HashSet<uint> PositiveStatuses = [];
    public readonly HashSet<uint> SpecialStatuses = [];
    public readonly HashSet<uint> DispelableIcons = [];
    public readonly Dictionary<uint, uint> IconStackCounts = [];
    public nint HoveringOver = 0;
    public List<nint> CancelRequests = [];
    public bool WasRightMousePressed = false;
    public bool NewMethod = true;
    private nint TooltipMemory;

    public CommonProcessor()
    {
        foreach (var x in Svc.Data.GetExcelSheet<Status>())
        {
            if (IconStackCounts.TryGetValue(x.Icon, out var count))
            {
                if (count < x.MaxStacks)
                {
                    IconStackCounts[x.Icon] = x.MaxStacks;
                }
            }
            else
            {
                IconStackCounts[x.Icon] = x.MaxStacks;
            }

            var fxpath = x.HitEffect.ValueNullable?.Location.ValueNullable?.Location.ExtractText();
            if (!fxpath.IsNullOrWhitespace() && !StatusEffectPaths.Contains(fxpath))
            {
                StatusEffectPaths.Add(fxpath);
            }

            if (NegativeStatuses.Contains(x.RowId) || PositiveStatuses.Contains(x.RowId) || SpecialStatuses.Contains(x.RowId)) continue;
            if (x.CanIncreaseRewards == 1)
            {
                SpecialStatuses.Add(x.RowId);
            }
            else if (x.StatusCategory == 1)
            {
                PositiveStatuses.Add(x.RowId);
            }
            else if (x.StatusCategory == 2)
            {
                NegativeStatuses.Add(x.RowId);
                DispelableIcons.Add(x.Icon);
                for (var i = 1; i < x.MaxStacks; i++)
                {
                    DispelableIcons.Add((uint)(x.Icon + i));
                }
            }
        }

        new EzFrameworkUpdate(Tick);
        PartyListProcessor = new();
        StatusCustomProcessor = new();
        TargetInfoProcessor = new();
        FocusTargetInfoProcessor = new();
        StatusProcessor = new();
        TargetInfoBuffDebuffProcessor = new();
        TooltipMemory = Marshal.AllocHGlobal(2 * 1024);
        FlyPopupTextProcessor = new();
    }

    public void Dispose()
    {
        HideAll();
        PartyListProcessor.Dispose();
        StatusCustomProcessor.Dispose();
        TargetInfoProcessor.Dispose();
        FocusTargetInfoProcessor.Dispose();
        StatusProcessor.Dispose();
        TargetInfoBuffDebuffProcessor.Dispose();
        FlyPopupTextProcessor.Dispose();
        Marshal.FreeHGlobal(TooltipMemory);
    }

    public void HideAll()
    {
        PartyListProcessor.HideAll();
        TargetInfoProcessor.HideAll();
        FocusTargetInfoProcessor.HideAll();
        StatusCustomProcessor.HideAll();
        StatusProcessor.HideAll();
        TargetInfoBuffDebuffProcessor.HideAll();
    }

    // Can revise this if we can properly track when everything happens, but until then it should be still
    // logical to perform processes in ticks.
    // (Could be handled as logic functions if we can track exactly when these specific events occur though)
    private unsafe void Tick()
    {
        // List of VFX that should be handled by the StatusHitEffect.
        List<(nint PlayerAddr, string customPath)> SHECandidates = [];

        if (HoveringOver != 0)
        {
            if (IsKeyPressed(LimitedKeys.LeftMouseButton)) WasRightMousePressed = false;
            if (IsKeyPressed(LimitedKeys.RightMouseButton)) WasRightMousePressed = true;
        }

        // Iterate through all tracked status managers.
        foreach (var (ownerNameWorld, sm) in C.StatusManagers)
        {
            var removed = new List<MyStatus>();
            var doChainApply = new List<MyStatus>();

            foreach (var x in sm.Statuses)
            {
                if (x.ClickedOff && sm.LockedIds.Contains(x.GUID))
                {
                    x.ClickedOff = false;
                    continue;
                }

                // Deterministic Logic
                if (x.ShouldExpireOnChain()) x.ExpiresAt = 0;
                if (x.HadNaturalTimerFalloff() && x.ChainTrigger is ChainTrigger.TimerExpired) x.ApplyChain = true;

                // Get the expire time.
                bool timeExpired = x.ExpiresAt - Utils.Time <= 0;
                                
                // Process status removal.
                if (timeExpired || x.ClickedOff)
                {
                    EnsureRemTextWasShown(sm, x, SHECandidates);
                    removed.Add(x);
                }
                else
                {
                    EnsureAddTextWasShown(sm, x);
                }
                // Mark the status to apply the chain, then reset the flag.
                if (x.ApplyChain)
                {
                    doChainApply.Add(x);
                    x.ApplyChain = false;
                }
            }

            HandleSHECandidates();

            // Now process the removal of all statuses marked.
            // (This allows for chains to be applied without removing original if desired)
            if (removed.Count > 0)
            {
                foreach (var status in removed) sm.Remove(status);
            }

            // Handle any status chaining logic.
            if (doChainApply.Count > 0)
            {
                foreach (var status in doChainApply) HandleStatusChaining(sm, status);
            }

            // Handle any other SHECandidates processing removed and chain applications.
            if (removed.Count > 0 || doChainApply.Count > 0) HandleSHECandidates();

            // Handle event firing.
            if (sm.NeedFireEvent)
            {
                sm.NeedFireEvent = false;
                // If the status manager owner exists, we can mark them as modified.
                if (sm.Owner != null)
                {
                    try
                    {
                        P.IPCProcessor.StatusManagerModified((nint)sm.Owner);
                    }
                    catch (Exception e)
                    {
                        PluginLog.Warning($"Something went wrong on StatusManagerModified IPCEvent!\n{e.Message}\n" +
                            $"One of your Plugins may have outdated IPC parameters for this IPCEvent");
                    }
                }
            }
        }

        // Clear any remaining Cancel Requests not yet processed before iterating SHECandidates
        CancelRequests.Clear();

        // Helper function to process the SHECandidates.
        void HandleSHECandidates()
        {
            foreach (var x in SHECandidates)
            {
                Character* chara = (Character*)x.PlayerAddr;
                if (ShouldSpawnHitEffect(chara, x.customPath))
                {
                    PluginLog.Debug($"StatusHitEffect on: {chara->NameString} / {x.customPath}");
                    if (x.customPath == "kill")
                    {
                        P.Memory.SpawnSHE("dk04ht_canc0h", x.PlayerAddr, x.PlayerAddr, -1, char.MinValue, 0, char.MinValue);
                    }
                    else
                    {
                        P.Memory.SpawnSHE(x.customPath, x.PlayerAddr, x.PlayerAddr, -1, char.MinValue, 0, char.MinValue);
                    }
                }
                else
                {
                    PluginLog.Debug($"Skipping SHE on {chara->NameString} / {x.customPath}");
                }
            }
            SHECandidates.Clear();
        }

        void HandleStatusChaining(MyStatusManager manager, MyStatus cur)
        {
            // Search for the chained status.
            foreach (var s in C.SavedStatuses)
            {
                if (s.GUID != cur.ChainedStatus) continue;

                int oldMax = P.CommonProcessor.IconStackCounts.TryGetValue((uint)cur.IconID, out var oCount) ? (int)oCount : 1;

                // Aquire the new chained status to be applied.
                MyStatus? newStatus = manager.AddOrUpdate(s.PrepareToApply(s.Persistent ? PrepareOptions.Persistent : PrepareOptions.NoOption), UpdateSource.StatusTuple);
                // If the new status if not valid just fail this process.
                if (newStatus is null) return;

                // Get the new max stacks, and if stackable, transfer stack logic.
                int newMaxStacks = P.CommonProcessor.IconStackCounts.TryGetValue((uint)newStatus.IconID, out var nCount) ? (int)nCount : 1;
                if (newMaxStacks > 1)
                {
                    if (cur.Modifiers.Has(Modifiers.StacksCarryToChain))
                    {
                        var toCarryOver = (cur.Stacks + cur.StackSteps) - oldMax;
                        // If the new status had a stack increase it would be doing that increase + this, so we need to subtract that addition.
                        newStatus.Stacks = Math.Min(newStatus.Stacks - newStatus.StackSteps + toCarryOver, newMaxStacks);
                    }
                    else if (cur.Modifiers.Has(Modifiers.StacksMoveToChain))
                    {
                        newStatus.Stacks = Math.Min(oldMax, newMaxStacks);
                    }
                }

                // Fix ensuring cap is hit when the chain trigger is max stacks.
                if (cur.ChainTrigger is ChainTrigger.HitMaxStacks) cur.Stacks = oldMax;

                // Ensure the add text is shown for this newly chained status, and then break out.
                EnsureAddTextWasShown(manager, s);
                break;
            }
        }

        void EnsureAddTextWasShown(MyStatusManager manager, MyStatus status)
        {
            if (manager.AddTextShown.Contains(status.GUID))
                return;

            if (P.CanModifyUI() && manager.OwnerValid)
            {
                if (manager.Owner->CanSpawnFlyText())
                {
                    FlyPopupTextProcessor.Enqueue(new(status, true, manager.Owner->EntityId));
                }
                if (manager.Owner->CanSpawnVFX())
                {
                    if (!SHECandidates.Any(s => s.PlayerAddr == (nint)manager.Owner))
                    {
                        // PluginLog.Debug($"Adding text for someone");
                        if (status.CustomFXPath.IsNullOrWhitespace())
                        {
                            SHECandidates.Add(((nint)manager.Owner, Utils.FindVFXPathByIconID((uint)status.IconID)));
                        }
                        else
                        {
                            SHECandidates.Add(((nint)manager.Owner, status.CustomFXPath));
                        }
                    }
                }
            }
            manager.AddTextShown.Add(status.GUID);
        }

        void EnsureRemTextWasShown(MyStatusManager manager, MyStatus status, List<(nint PlayerAddr, string customPath)> SHECandidates)
        {
            if (manager.RemTextShown.Contains(status.GUID))
                return;
            if (P.CanModifyUI() && manager.Owner != null)
            {
                if (manager.Owner->CanSpawnFlyText())
                {
                    FlyPopupTextProcessor.Enqueue(new(status, false, manager.Owner->EntityId));
                }
                if (manager.Owner->CanSpawnVFX())
                {
                    if (!SHECandidates.Any(s => s.PlayerAddr == (nint)manager.Owner))
                    {
                        SHECandidates.Add(((nint)manager.Owner, "kill"));
                    }
                }
            }
            manager.RemTextShown.Add(status.GUID);
        }
    }

    private static unsafe bool ShouldSpawnHitEffect(Character* chara, string vfxPath)
    {
        // For some really weird reason whenever this is included the plugin just randomly decides if it wants to spawn any SHE at all.
        // if (!C.EnableSHE) return false;

        if (!C.RestrictSHE) return true;

        if ((nint)chara == LocalPlayer.Address) return true;

        if (Utils.GetFriendlist().Contains(chara->GetNameWithWorld())) return true;

        if (UniversalParty.Members.Any(z => z.NameWithWorld == chara->GetNameWithWorld())) return true;

        if (Vector3.Distance(LocalPlayer.Character->Position, chara->Position) < 15f) return true;

        return false;
    }

    // Update to include the status manager parent.
    public void SetIcon(AtkUnitBase* addon, AtkResNode* container, MyStatus status)
    {
        if (!container->IsVisible())
        {
            container->NodeFlags ^= NodeFlags.Visible;
        }

        P.Memory.AtkComponentIconText_LoadIconByID(container->GetAsAtkComponentNode()->Component, (int)status.AdjustedIconID);

        var dispelNode = container->GetAsAtkComponentNode()->Component->UldManager.NodeList[0];

        // Make it not marked as dispelable if it is not part of the dispelable icons cache.
        if (status.Modifiers.Has(Modifiers.CanDispel) && !DispelableIcons.Contains((uint)status.IconID))
        {
            status.Modifiers.Set(Modifiers.CanDispel, false);
        }

        // Toggle visibility if it does not match the dispel nodes visibility
        if (status.Modifiers.Has(Modifiers.CanDispel) != dispelNode->IsVisible())
        {
            dispelNode->NodeFlags ^= NodeFlags.Visible;
        }

        var textNode = container->GetAsAtkComponentNode()->Component->UldManager.NodeList[2];
        var timerText = "";
        if (status.ExpiresAt != long.MaxValue)
        {
            var rem = status.ExpiresAt - Utils.Time;
            timerText = rem > 0 ? GetTimerText(rem) : "";
        }

        if (timerText != null)
        {
            if (!textNode->IsVisible()) textNode->NodeFlags ^= NodeFlags.Visible;
        }

        var t = textNode->GetAsAtkTextNode();
        t->SetText((timerText ?? SeString.Empty).Encode());
        if (status.Applier == Player.NameWithWorld)
        {
            t->TextColor = CreateColor(0xc9ffe4ff);
            t->EdgeColor = CreateColor(0x0a5f24ff);
            t->BackgroundColor = CreateColor(0);
        }
        else
        {
            t->TextColor = CreateColor(0xffffffff);
            t->EdgeColor = CreateColor(0x333333ff);
            t->BackgroundColor = CreateColor(0);
        }
        var addr = (nint)(container->GetAsAtkComponentNode()->Component);
        // PluginLog.Debug($"- = - {MemoryHelper.ReadStringNullTerminated((nint)addon->Name)} - = -");
        if (HoveringOver == addr && status.TooltipShown == -1)
        {
            //PluginLog.Debug($"Trigger 0:{addr:X16} / {Utils.Frame} / {GetCallStackID()}");
            C.StatusManagers.Each(f => f.Value.Statuses.Each(z => z.TooltipShown = -1));
            status.TooltipShown = addon->Id;
            AtkStage.Instance()->TooltipManager.HideTooltip(addon->Id);
            var str = status.Title;
            if (status.Description != "")
            {
                str += $"\n{status.Description}";
            }
            MemoryHelper.WriteSeString(TooltipMemory, Utils.ParseBBSeString(str));
            AtkStage.Instance()->TooltipManager.ShowTooltip((ushort)addon->Id, container, (byte*)TooltipMemory);
        }
        if (status.TooltipShown == addon->Id && HoveringOver != addr)
        {
            //PluginLog.Debug($"Trigger 1 {addr:X16} / {Utils.Frame} / {GetCallStackID()}");
            status.TooltipShown = -1;
            if (HoveringOver == 0)
            {
                //PluginLog.Debug($"Trigger 2 / {Utils.Frame} / {GetCallStackID()}");
                AtkStage.Instance()->TooltipManager.HideTooltip(addon->Id);
            }
        }

        // If we requested to cancel this via a right click, then flag it for this.
        if (CancelRequests.Remove(addr))
        {
            // Move hiding the mouseover to here so we can reference the status that is removed.
            var name = addon->NameString;
            if (name.StartsWith("_StatusCustom") || name == "_Status") status.ClickedOff = true;
        }
    }

    public string GetTimerText(long rem)
    {
        var seconds = MathF.Ceiling((float)rem / 1000f);
        if (seconds <= 59) return seconds.ToString();
        var minutes = MathF.Floor((float)seconds / 60f);
        if (minutes <= 59) return $"{minutes}m";
        var hours = MathF.Floor((float)minutes / 60f);
        if (hours <= 59) return $"{hours}h";
        var days = MathF.Floor((float)hours / 24f);
        if (days <= 9) return $"{days}d";
        return $">9d";
    }

    private ByteColor CreateColor(uint color)
    {
        color = BinaryPrimitives.ReverseEndianness(color);
        var ptr = &color;
        return *(ByteColor*)ptr;
    }
}
