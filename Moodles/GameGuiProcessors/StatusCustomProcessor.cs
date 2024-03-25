using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;

namespace Moodles.GameGuiProcessors;
public unsafe class StatusCustomProcessor : IDisposable
{
    public int NumStatuses0 = 0;
    public int NumStatuses1 = 0;
    public int NumStatuses2 = 0;

    public StatusCustomProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_StatusCustom0", OnStatusCustom0Update);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_StatusCustom1", OnStatusCustom1Update);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_StatusCustom2", OnStatusCustom2Update);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_StatusCustom0", OnStatusCustom0RequestedUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_StatusCustom1", OnStatusCustom1RequestedUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_StatusCustom2", OnStatusCustom2RequestedUpdate);
        if (Player.Available)
        {
            {
                if (TryGetAddonByName<AtkUnitBase>("_StatusCustom0", out var addon) && IsAddonReady(addon))
                {
                    this.OnStatusCustom0RequestedUpdate(AddonEvent.PostRequestedUpdate, new ArtificialAddonArgs(addon));
                }
            }
            {
                if (TryGetAddonByName<AtkUnitBase>("_StatusCustom1", out var addon) && IsAddonReady(addon))
                {
                    this.OnStatusCustom1RequestedUpdate(AddonEvent.PostRequestedUpdate, new ArtificialAddonArgs(addon));
                }
            }
            {
                if (TryGetAddonByName<AtkUnitBase>("_StatusCustom2", out var addon) && IsAddonReady(addon))
                {
                    this.OnStatusCustom2RequestedUpdate(AddonEvent.PostRequestedUpdate, new ArtificialAddonArgs(addon));
                }
            }
        }
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_StatusCustom0", OnStatusCustom0Update);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_StatusCustom1", OnStatusCustom1Update);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_StatusCustom2", OnStatusCustom2Update);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_StatusCustom0", OnStatusCustom0RequestedUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_StatusCustom1", OnStatusCustom1RequestedUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_StatusCustom2", OnStatusCustom2RequestedUpdate);
    }

    public void HideAll()
    {
        if (!Player.Available) return;

        {
            if (TryGetAddonByName<AtkUnitBase>("_StatusCustom0", out var addon) && IsAddonReady(addon))
            {
                var validStatuses = Utils.GetMyStatusManager(Player.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Positive);
                UpdateStatusCustom(addon, validStatuses, P.CommonProcessor.PositiveStatuses, NumStatuses0, true);
            }
        }
        {
            if (TryGetAddonByName<AtkUnitBase>("_StatusCustom1", out var addon) && IsAddonReady(addon))
            {
                var validStatuses = Utils.GetMyStatusManager(Player.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Negative);
                UpdateStatusCustom(addon, validStatuses, P.CommonProcessor.NegativeStatuses, NumStatuses1, true);
            }
        }
        {
            if (TryGetAddonByName<AtkUnitBase>("_StatusCustom2", out var addon) && IsAddonReady(addon))
            {
                var validStatuses = Utils.GetMyStatusManager(Player.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Special);
                UpdateStatusCustom(addon, validStatuses, P.CommonProcessor.SpecialStatuses, NumStatuses2, true);
            }
        }
    }

    private void OnStatusCustom0RequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        RequestedUpdateStatusCustom((AtkUnitBase*)args.Addon, ref NumStatuses0);
        InternalLog.Verbose($"StatusCustom0 Requested update: {NumStatuses0}");
    }

    private void OnStatusCustom1RequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        RequestedUpdateStatusCustom((AtkUnitBase*)args.Addon, ref NumStatuses1);
        InternalLog.Verbose($"StatusCustom1 Requested update: {NumStatuses1}");
    }

    private void OnStatusCustom2RequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        RequestedUpdateStatusCustom((AtkUnitBase*)args.Addon, ref NumStatuses2);
        InternalLog.Verbose($"StatusCustom2 Requested update: {NumStatuses2}");
    }

    void RequestedUpdateStatusCustom(AtkUnitBase* addon, ref int StatusCnt)
    {
        if (addon != null && IsAddonReady(addon) && P.CanModifyUI())
        {
            StatusCnt = 0;
            for (int i = 24; i >= 5; i--)
            {
                var c = addon->UldManager.NodeList[i];
                if (c->IsVisible)
                {
                    StatusCnt++;
                }
            }
        }
    }

    //permanent
    private void OnStatusCustom2Update(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        if (!Player.Available) return;
        if (!P.CanModifyUI()) return;
        //PluginLog.Verbose($"Post1 update {args.Addon:X16}");
        var validStatuses = Utils.GetMyStatusManager(Player.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Special);
        UpdateStatusCustom((AtkUnitBase*)args.Addon, validStatuses, P.CommonProcessor.SpecialStatuses, NumStatuses2);
    }

    //debuffs
    private void OnStatusCustom1Update(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        if (!Player.Available) return;
        if (!P.CanModifyUI()) return;
        //PluginLog.Verbose($"Post1 update {args.Addon:X16}");
        var validStatuses = Utils.GetMyStatusManager(Player.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Negative);
        UpdateStatusCustom((AtkUnitBase*)args.Addon, validStatuses, P.CommonProcessor.NegativeStatuses, NumStatuses1);
    }

    //buffs
    private void OnStatusCustom0Update(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        if (!Player.Available) return;
        if (!P.CanModifyUI()) return;
        //PluginLog.Verbose($"Post0 update {args.Addon:X16}");
        var validStatuses = Utils.GetMyStatusManager(Player.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Positive);
        UpdateStatusCustom((AtkUnitBase*)args.Addon, validStatuses, P.CommonProcessor.PositiveStatuses, NumStatuses0);
    }

    public void UpdateStatusCustom(AtkUnitBase* addon, IEnumerable<MyStatus> statuses, IEnumerable<uint> userStatuses, int StatusCnt, bool hideAll = false)
    {
        if (addon != null && IsAddonReady(addon))
        {
            int baseCnt;
            if (P.CommonProcessor.NewMethod)
            {
                baseCnt = 24 - StatusCnt;
            }
            else
            {
                baseCnt = 24 - Player.Object.StatusList.Count(x => x.StatusId != 0 && userStatuses.Contains(x.StatusId));
                if (Svc.Condition[ConditionFlag.Mounted] && MemoryHelper.ReadStringNullTerminated((nint)addon->Name) == "StatusCustom2") baseCnt--;
            }
            for (int i = baseCnt; i >= 5; i--)
            {
                var c = addon->UldManager.NodeList[i];
                if (c->IsVisible) c->NodeFlags ^= NodeFlags.Visible;
            }
            if (!hideAll)
            {
                foreach (var x in statuses)
                {
                    if (baseCnt < 5) break;
                    var rem = x.ExpiresAt - Utils.Time;
                    if (rem > 0)
                    {
                        SetIcon(addon, baseCnt, x);
                        baseCnt--;
                    }
                }
            }
        }
    }

    void SetIcon(AtkUnitBase* addon, int index, MyStatus status)
    {
        var container = addon->UldManager.NodeList[index];
        P.CommonProcessor.SetIcon(addon, container, status);
    }
}
