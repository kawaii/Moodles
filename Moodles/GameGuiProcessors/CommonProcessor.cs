using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.Interop;
using ECommons.MathHelpers;
using ECommons.PartyFunctions;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Moodles.Data;
using Moodles.GameGuiProcessors;

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
            if (!StatusEffectPaths.Contains(fxpath) && !fxpath.IsNullOrWhitespace())
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

    private void Tick()
    {
        // List of VFX that should be handled by the StatusHitEffect.
        List<(IPlayerCharacter Player, string customPath)> SHECandidates = [];

        if (HoveringOver != 0)
        {
            if (IsKeyPressed(LimitedKeys.LeftMouseButton)) WasRightMousePressed = false;
            if (IsKeyPressed(LimitedKeys.RightMouseButton)) WasRightMousePressed = true;
        }

        // Check all status managers for any statuses that need to be applied or removed.
        foreach (var statusManager in C.StatusManagers)
        {
            // track removed statuses, so we can reapply statuses marked for ApplyOnDispel safely.
            var removed = new List<MyStatus>();

            // Handle Status Apply/Remove logic.
            foreach (var x in statusManager.Value.Statuses)
            {
                var rem = x.ExpiresAt - Utils.Time;
                if (rem > 0)
                {
                    EnsureAddTextWasShown(statusManager.Value, x);
                }
                else
                {
                    EnsureRemTextWasShown(statusManager.Value, x);
                    removed.Add(x);
                }
            }

            // Process the SHECandidates for the initial pass.
            HandleSHECandidates();

            // Check removed statuses for any that need to be applied another status on dispel.
            if (removed.Count > 0)
            {
                // Process the removals.
                foreach (var status in removed)
                {
                    statusManager.Value.Remove(status);

                    // If there is an additional status to apply on dispel, apply it, and then ensure its add text is shown.
                    if (statusManager.Value.Owner?.ObjectIndex == 0 && status.StatusOnDispell != Guid.Empty)
                    {
                        foreach (var s in C.SavedStatuses)
                        {
                            if (s.GUID != status.StatusOnDispell) continue;

                            statusManager.Value.AddOrUpdate(s.PrepareToApply(s.Persistent ? PrepareOptions.Persistent : PrepareOptions.NoOption), UpdateSource.StatusTuple);
                            EnsureAddTextWasShown(statusManager.Value, s);
                            break;
                        }
                    }
                }
                // Process the SHECandidates for any statuses that were reapplied after removal, if any.
                HandleSHECandidates();
            }

            // If the Status manager has changed and needs to fire an event, handle it here.
            if (statusManager.Value.NeedFireEvent)
            {
                statusManager.Value.NeedFireEvent = false;
                if (Svc.Objects.TryGetFirst(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == statusManager.Key, out var pc))
                {
                    P.IPCProcessor.StatusManagerModified((IPlayerCharacter)pc);
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
                if (!C.RestrictSHE || x.Player.AddressEquals(Player.Object) || Utils.GetFriendlist().Contains(x.Player.GetNameWithWorld()) || UniversalParty.Members.Any(z => z.NameWithWorld == x.Player.GetNameWithWorld()) || Vector3.Distance(Player.Object.Position, x.Player.Position) < 15f)
                {
                    PluginLog.Debug($"StatusHitEffect on: {x.Player} / {x.customPath}");
                    if (x.customPath == "kill")
                    {
                        P.Memory.SpawnSHE("dk04ht_canc0h", x.Player.Address, x.Player.Address, -1, char.MinValue, 0, char.MinValue);
                    }
                    else
                    {
                        P.Memory.SpawnSHE(x.customPath, x.Player.Address, x.Player.Address, -1, char.MinValue, 0, char.MinValue);
                    }
                }
                else
                {
                    PluginLog.Debug($"Skipping SHE on {x.Player} / {x.customPath}");
                }
            }
            SHECandidates.Clear();
        }

        // Internal helper function that handles logic for AddText & VFX
        void EnsureAddTextWasShown(MyStatusManager manager, MyStatus status)
        {
            if (manager.AddTextShown.Contains(status.GUID))
                return;

            if (P.CanModifyUI() && manager.Owner != null)
            {
                if (Utils.CanSpawnFlytext(manager.Owner))
                {
                    FlyPopupTextProcessor.Enqueue(new(status, true, manager.Owner.EntityId));
                }
                if (Utils.CanSpawnVFX(manager.Owner))
                {
                    if (!SHECandidates.Any(s => s.Player.AddressEquals(manager.Owner)))
                    {
                        if (status.CustomFXPath.IsNullOrWhitespace())
                        {
                            SHECandidates.Add((manager.Owner, Utils.FindVFXPathByIconID((uint)status.IconID)));
                        }
                        else
                        {
                            SHECandidates.Add((manager.Owner, status.CustomFXPath));
                        }
                    }
                }
            }
            manager.AddTextShown.Add(status.GUID);
        }

        // Internal helper function that handles logic for RemText & kill VFX
        void EnsureRemTextWasShown(MyStatusManager manager, MyStatus status)
        {
            if (manager.RemTextShown.Contains(status.GUID))
                return;

            if (P.CanModifyUI() && manager.Owner != null)
            {
                if (Utils.CanSpawnFlytext(manager.Owner))
                {
                    FlyPopupTextProcessor.Enqueue(new(status, false, manager.Owner.EntityId));
                }
                if (Utils.CanSpawnVFX(manager.Owner))
                {
                    if (!SHECandidates.Any(s => s.Player.AddressEquals(manager.Owner)))
                    {
                        SHECandidates.Add((manager.Owner, "kill"));
                    }
                }
            }
            manager.RemTextShown.Add(status.GUID);
        }
    }

    public void SetIcon(AtkUnitBase* addon, AtkResNode* container, MyStatus status)
    {
        if (!container->IsVisible()) container->NodeFlags ^= NodeFlags.Visible;
        P.Memory.AtkComponentIconText_LoadIconByID(container->GetAsAtkComponentNode()->Component, (int)status.AdjustedIconID);
        var dispelNode = container->GetAsAtkComponentNode()->Component->UldManager.NodeList[0];
        if (status.Dispelable && !DispelableIcons.Contains((uint)status.IconID)) status.Dispelable = false;
        if (status.Dispelable != dispelNode->IsVisible())
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
        //PluginLog.Debug($"- = - {MemoryHelper.ReadStringNullTerminated((nint)addon->Name)} - = -");
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
        if (CancelRequests.Remove(addr))
        {
            var name = addon->NameString;
            if (name.StartsWith("_StatusCustom") || name == "_Status") status.ExpiresAt = 0;
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
        color = Endianness.SwapBytes(color);
        var ptr = &color;
        return *(ByteColor*)ptr;
    }
}
