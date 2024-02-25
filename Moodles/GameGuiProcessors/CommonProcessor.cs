using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.Interop;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Moodles.Data;
using Moodles.GameGuiProcessors;
using Moodles.VfxManager;

namespace Moodles.Processors;
public unsafe class CommonProcessor : IDisposable
{
    public PartyListProcessor PartyListProcessor;
    public StatusCustomProcessor StatusCustomProcessor;
    public TargetInfoProcessor TargetInfoProcessor;
    public FocusTargetInfoProcessor FocusTargetInfoProcessor;
    public StatusProcessor StatusProcessor;
    public FlyPopupTextProcessor FlyPopupTextProcessor;
    public readonly HashSet<uint> NegativeStatuses = [];
    public readonly HashSet<uint> PositiveStatuses = [];
    public readonly HashSet<uint> SpecialStatuses = [];
    public readonly HashSet<uint> DispelableIcons = [];
    public readonly Dictionary<uint, uint> IconStackCounts = [];
    public nint HoveringOver = 0;
    public List<nint> CancelRequests = [];
    public bool WasRightMousePressed = false;
    public bool NewMethod = true;
    nint TooltipMemory;

    public List<(MyStatusManager StatusManager, MyStatus Status)> CleanupQueue = [];

    public CommonProcessor()
    {
        foreach(var x in Svc.Data.GetExcelSheet<Status>())
        {
            if(IconStackCounts.TryGetValue(x.Icon, out var count))
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
            if (NegativeStatuses.Contains(x.RowId) || PositiveStatuses.Contains(x.RowId) || SpecialStatuses.Contains(x.RowId)) continue;
            if (x.CanIncreaseRewards == 1)
            {
                SpecialStatuses.Add(x.RowId);
            }
            else if(x.StatusCategory == 1)
            {
                PositiveStatuses.Add(x.RowId);
            }
            else if(x.StatusCategory == 2)
            {
                NegativeStatuses.Add(x.RowId);
                DispelableIcons.Add(x.Icon);
                for (int i = 1; i < x.MaxStacks; i++)
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
        TooltipMemory = Marshal.AllocHGlobal(2*1024);
        FlyPopupTextProcessor = new();
    }

    public void Dispose()
    {
        PartyListProcessor.Dispose();
        StatusCustomProcessor.Dispose();
        TargetInfoProcessor.Dispose();
        FocusTargetInfoProcessor.Dispose();
        StatusProcessor.Dispose();
        FlyPopupTextProcessor.Dispose();
        Marshal.FreeHGlobal(TooltipMemory);
    }

    public void HideAll()
    {
        this.PartyListProcessor.HideAll();
        this.TargetInfoProcessor.HideAll();
        this.FocusTargetInfoProcessor.HideAll();
        this.StatusCustomProcessor.HideAll();
        this.StatusProcessor.HideAll();
    }

    private void Tick()
    {
        VfxSpawn.Tick();
        List<(PlayerCharacter Player, string VFX)> VFXCandidates = [];
        if (HoveringOver != 0)
        {
            if (IsKeyPressed(LimitedKeys.LeftMouseButton)) WasRightMousePressed = false;
            if (IsKeyPressed(LimitedKeys.RightMouseButton)) WasRightMousePressed = true;
        }
        foreach (var x in CleanupQueue)
        {
            x.StatusManager.AddTextShown.Remove(x.Status.GUID);
            x.StatusManager.RemTextShown.Remove(x.Status.GUID);
            x.StatusManager.Statuses.Remove(x.Status);
        }
        CleanupQueue.Clear();
        foreach (var statusManager in C.StatusManagers)
        {
            foreach (var x in statusManager.Value.Statuses)
            {
                var rem = x.ExpiresAt - Utils.Time;
                if (rem > 0)
                {
                    if (!statusManager.Value.AddTextShown.Contains(x.GUID))
                    {
                        if (P.CanModifyUI() && TryFindIndex(statusManager.Key, out var index, out var pc) && Utils.CanSpawnVfxFlytext(pc))
                        {
                            /*if (statusManager.Key == Player.NameWithWorld || x.Applier == Player.NameWithWorld)
                            {
                                var col = x.Type == StatusType.Negative ? 0xff00008A : 0xFF005D2A;
                                var type = x.Type == StatusType.Negative ? FlyTextKind.Debuff : FlyTextKind.Buff;
                                if (index != -1) Svc.FlyText.AddFlyText(type, (uint)index, 0, 0, new SeStringBuilder().AddText($"+ ").Append(Utils.ParseBBSeString(x.Title)).Build(), "", col, (uint)x.AdjustedIconID, 0);
                            }*/
                            FlyPopupTextProcessor.Enqueue(new(x, true, pc.ObjectId));
                            if (x.Type == StatusType.Negative)
                            {
                                VFXCandidates.Add((pc, "vfx/common/eff/dk05th_stdn0t.avfx"));
                            }
                            else
                            {
                                VFXCandidates.Add((pc, "vfx/common/eff/dk05th_stup0t.avfx"));
                            }
                        }
                        statusManager.Value.AddTextShown.Add(x.GUID);
                    }
                }
                else
                {
                    if (!statusManager.Value.RemTextShown.Contains(x.GUID))
                    {
                        if (P.CanModifyUI() && TryFindIndex(statusManager.Key, out var index, out var pc) && Utils.CanSpawnVfxFlytext(pc))
                        {
                            /*if (statusManager.Key == Player.NameWithWorld || x.Applier == Player.NameWithWorld)
                            {
                                var col = 0xFF454545;
                                var type = x.Type == StatusType.Negative ? FlyTextKind.DebuffFading : FlyTextKind.BuffFading;
                                if (index != -1) Svc.FlyText.AddFlyText(type, (uint)index, 0, 0, new SeStringBuilder().AddText($"- ").Append(Utils.ParseBBSeString(x.Title)).Build(), "", col, (uint)x.AdjustedIconID, 0);
                            }*/
                            FlyPopupTextProcessor.Enqueue(new(x, false, pc.ObjectId));
                            VFXCandidates.Add((pc, "vfx/common/eff/dk04ht_canc0h.avfx"));
                        }
                        statusManager.Value.RemTextShown.Add(x.GUID);
                    }
                    CleanupQueue.Add((statusManager.Value, x));
                }
            }
            if (statusManager.Value.NeedFireEvent)
            {
                statusManager.Value.NeedFireEvent = false;
                if (Svc.Objects.TryGetFirst(x => x is PlayerCharacter pc && pc.GetNameWithWorld() == statusManager.Key, out var pc))
                {
                    P.IPCProcessor.FireStatusManagerChange((PlayerCharacter)pc);
                }
            }
        }
        CancelRequests.Clear();
        if (VFXCandidates.Count > 0 && C.EnableVFX && EzThrottler.Throttle("SpawnVFX", 100))
        {
            var bestCandidate = VFXCandidates.OrderBy(x => Vector3.DistanceSquared(Player.Object.Position, x.Player.Position)).First();
            PluginLog.Debug($"Spawning {bestCandidate.VFX} on {bestCandidate.Player}");
            VfxSpawn.SpawnOn(bestCandidate.Player, bestCandidate.VFX);
        }
    }

    bool TryFindIndex(string name, out int index, out PlayerCharacter pcr)
    {
        if(name == Player.NameWithWorld)
        {
            index = 1;
            pcr = Player.Object;
            return true;
        }
        else
        {
            foreach (var x in Svc.Objects)
            {
                if(x is PlayerCharacter pc && pc.IsTargetable && pc.GetNameWithWorld() == name)
                {
                    index = -1;
                    pcr = pc;
                    return true;
                }
            }
        }
        index = -1;
        pcr = null;
        return false;
    }

    public void SetIcon(AtkUnitBase* addon, AtkResNode* container, MyStatus status)
    {
        if (!container->IsVisible) container->NodeFlags ^= NodeFlags.Visible;
        P.Memory.AtkComponentIconText_LoadIconByID(container->GetAsAtkComponentNode()->Component, (int)status.AdjustedIconID);
        var dispelNode = container->GetAsAtkComponentNode()->Component->UldManager.NodeList[0];
        if (status.Dispelable && !DispelableIcons.Contains((uint)status.IconID)) status.Dispelable = false;
        if (status.Dispelable != dispelNode->IsVisible)
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
            if (!textNode->IsVisible) textNode->NodeFlags ^= NodeFlags.Visible;
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
            status.TooltipShown = addon->ID;
            AtkStage.GetSingleton()->TooltipManager.HideTooltip(addon->ID);
            var str = status.Title;
            if(status.Description != "")
            {
                str += $"\n{status.Description}";
            }
            MemoryHelper.WriteSeString(TooltipMemory, Utils.ParseBBSeString(str));
            AtkStage.GetSingleton()->TooltipManager.ShowTooltip((ushort)addon->ID, container, (byte*)TooltipMemory);
        }
        if(status.TooltipShown == addon->ID && HoveringOver != addr)
        {
            //PluginLog.Debug($"Trigger 1 {addr:X16} / {Utils.Frame} / {GetCallStackID()}");
            status.TooltipShown = -1;
            if(HoveringOver == 0)
            {
                //PluginLog.Debug($"Trigger 2 / {Utils.Frame} / {GetCallStackID()}");
                AtkStage.GetSingleton()->TooltipManager.HideTooltip(addon->ID);
            }
        }
        if (CancelRequests.Contains(addr))
        {
            CancelRequests.Remove(addr);
            var name = MemoryHelper.ReadStringNullTerminated((nint)addon->Name);
            if (name.StartsWith("_StatusCustom") || name == "_Status")
            {
                status.ExpiresAt = 0;
                P.IPCProcessor.FireStatusManagerChange(Player.Object);
            }
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

    ByteColor CreateColor(uint color)
    {
        color = Endianness.SwapBytes(color);
        var ptr = &color;
        return *(ByteColor*)ptr;
    }
}
