using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;

namespace Moodles.GameGuiProcessors;
public unsafe class StatusCustomProcessor : IDisposable
{
    public int NumStatuses0 = 0;
    public int NumStatuses1 = 0;
    public int NumStatuses2 = 0;

    int lastStatusCount = 0;
    bool statusCountLessened = false;

    public StatusCustomProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_StatusCustom0", OnStatusCustom0Update);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_StatusCustom1", OnStatusCustom1Update);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_StatusCustom2", OnStatusCustom2Update);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_StatusCustom0", OnStatusCustom0RequestedUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_StatusCustom1", OnStatusCustom1RequestedUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_StatusCustom2", OnStatusCustom2RequestedUpdate);
        if(Player.Available)
        {
            if(TryGetAddonByName<AtkUnitBase>("_StatusCustom0", out var addon0) && IsAddonReady(addon0))
            {
                Custom0RequestedUpdate(addon0);
            }

            if(TryGetAddonByName<AtkUnitBase>("_StatusCustom1", out var addon1) && IsAddonReady(addon1))
            {
                Custom1RequestedUpdate(addon1);
            }

            if(TryGetAddonByName<AtkUnitBase>("_StatusCustom2", out var addon2) && IsAddonReady(addon2))
            {
                Custom2RequestedUpdate(addon2);
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
        if(!Player.Available) return;
        
        if(TryGetAddonByName<AtkUnitBase>("_StatusCustom0", out var addon0) && IsAddonReady(addon0))
        {
            var validStatuses = Utils.GetMyStatusManager(LocalPlayer.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Positive);
            UpdateStatusCustom(addon0, validStatuses, P.CommonProcessor.PositiveStatuses, NumStatuses0, true);
        }

        if(TryGetAddonByName<AtkUnitBase>("_StatusCustom1", out var addon1) && IsAddonReady(addon1))
        {
            var validStatuses = Utils.GetMyStatusManager(LocalPlayer.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Negative);
            UpdateStatusCustom(addon1, validStatuses, P.CommonProcessor.NegativeStatuses, NumStatuses1, true);
        }

        if(TryGetAddonByName<AtkUnitBase>("_StatusCustom2", out var addon2) && IsAddonReady(addon2))
        {
            var validStatuses = Utils.GetMyStatusManager(LocalPlayer.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Special);
            UpdateStatusCustom(addon2, validStatuses, P.CommonProcessor.SpecialStatuses, NumStatuses2, true);
        }
    }

    // Func helper to get around 7.4's internal AddonArgs while removing ArtificialAddonArgs usage 
    private void OnStatusCustom0RequestedUpdate(AddonEvent t, AddonArgs args) => Custom0RequestedUpdate((AtkUnitBase*)args.Addon.Address);
    private void OnStatusCustom1RequestedUpdate(AddonEvent t, AddonArgs args) => Custom1RequestedUpdate((AtkUnitBase*)args.Addon.Address);
    private void OnStatusCustom2RequestedUpdate(AddonEvent t, AddonArgs args) => Custom2RequestedUpdate((AtkUnitBase*)args.Addon.Address);

    private void Custom0RequestedUpdate(AtkUnitBase* addonBase)
    {
        if(P == null) return;
        AddonRequestedUpdate(addonBase, ref NumStatuses0);
        InternalLog.Verbose($"StatusCustom0 Requested update: {NumStatuses0}");
    }

    private void Custom1RequestedUpdate(AtkUnitBase* addonBase)
    {
        if(P == null) return;
        AddonRequestedUpdate(addonBase, ref NumStatuses1);
        InternalLog.Verbose($"StatusCustom1 Requested update: {NumStatuses1}");
    }

    private void Custom2RequestedUpdate(AtkUnitBase* addonBase)
    {
        if(P == null) return;
        AddonRequestedUpdate(addonBase, ref NumStatuses2);
        InternalLog.Verbose($"StatusCustom2 Requested update: {NumStatuses2}");
    }

    private void AddonRequestedUpdate(AtkUnitBase* addon, ref int StatusCnt)
    {
        if(addon != null && IsAddonReady(addon) && P.CanModifyUI())
        {
            StatusCnt = 0;
            for(var i = 24; i >= 5; i--)
            {
                var c = addon->UldManager.NodeList[i];
                if(c->IsVisible())
                {
                    StatusCnt++;
                }
            }

            if (lastStatusCount != StatusCnt)
            {
                if (StatusCnt < lastStatusCount)
                {
                    statusCountLessened = true;
                }
                lastStatusCount = StatusCnt;
            }
        }
    }

    //permanent
    private void OnStatusCustom2Update(AddonEvent type, AddonArgs args)
    {
        if(P == null) return;
        if(!Player.Available) return;
        if(!P.CanModifyUI()) return;
        //PluginLog.Verbose($"Post1 update {args.Addon:X16}");
        var validStatuses = Utils.GetMyStatusManager(LocalPlayer.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Special);
        UpdateStatusCustom((AtkUnitBase*)args.Addon.Address, validStatuses, P.CommonProcessor.SpecialStatuses, NumStatuses2);
    }

    //debuffs
    private void OnStatusCustom1Update(AddonEvent type, AddonArgs args)
    {
        if(P == null) return;
        if(!Player.Available) return;
        if(!P.CanModifyUI()) return;
        //PluginLog.Verbose($"Post1 update {args.Addon:X16}");
        var validStatuses = Utils.GetMyStatusManager(LocalPlayer.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Negative);
        UpdateStatusCustom((AtkUnitBase*)args.Addon.Address, validStatuses, P.CommonProcessor.NegativeStatuses, NumStatuses1);
    }

    //buffs
    private void OnStatusCustom0Update(AddonEvent type, AddonArgs args)
    {
        if(P == null) return;
        if(!Player.Available) return;
        if(!P.CanModifyUI()) return;
        //PluginLog.Verbose($"Post0 update {args.Addon:X16}");
        var validStatuses = Utils.GetMyStatusManager(LocalPlayer.NameWithWorld).Statuses.Where(x => x.Type == StatusType.Positive);
        UpdateStatusCustom((AtkUnitBase*)args.Addon.Address, validStatuses, P.CommonProcessor.PositiveStatuses, NumStatuses0);
    }

    // The common logic method with all statuses of a defined type in the player's status manager.
    public void UpdateStatusCustom(AtkUnitBase* addon, IEnumerable<MyStatus> statuses, IEnumerable<uint> userStatuses, int StatusCnt, bool hideAll = false)
    {
        if(addon != null && IsAddonReady(addon))
        {
            int baseCnt;
            if(P.CommonProcessor.NewMethod)
            {
                baseCnt = 24 - StatusCnt;
            }
            else
            {
                baseCnt = 24 - LocalPlayer.StatusList.Count(x => x.StatusId != 0 && userStatuses.Contains(x.StatusId));
                if(Svc.Condition[ConditionFlag.Mounted] && addon->NameString == "StatusCustom2") baseCnt--;
            }
            for(var i = baseCnt; i >= 5; i--)
            {
                var c = addon->UldManager.NodeList[i];
                if (c->IsVisible()) c->NodeFlags ^= NodeFlags.Visible;
            }
            if(!hideAll)
            {
                foreach(var x in statuses)
                {
                    if(baseCnt < 5) break;
                    var rem = x.ExpiresAt - Utils.Time;
                    if (rem > 0)
                    {
                        if (statusCountLessened)
                        {
                            statusCountLessened = false;
                            SetIcon(addon, baseCnt - P.CommonProcessor.CancelRequests.Count, x);
                        }
                        else
                        {
                            SetIcon(addon, baseCnt, x);
                        }
                        baseCnt--;
                    }
                }
            }
        }
    }

    private void SetIcon(AtkUnitBase* addon, int index, MyStatus status)
    {
        var container = addon->UldManager.NodeList[index];
        P.CommonProcessor.SetIcon(addon, container, status);
    }
}
