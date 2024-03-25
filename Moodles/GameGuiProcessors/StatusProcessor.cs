using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;

namespace Moodles.GameGuiProcessors;
public unsafe class StatusProcessor : IDisposable
{
    public int NumStatuses = 0;

    public StatusProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_Status", OnStatusUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_Status", OnStatusRequestedUpdate);
        if (Player.Available && TryGetAddonByName<AtkUnitBase>("_Status", out var addon) && IsAddonReady(addon))
        {
            this.OnStatusRequestedUpdate(AddonEvent.PostRequestedUpdate, new ArtificialAddonArgs(addon));
        }
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_Status", OnStatusUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_Status", OnStatusRequestedUpdate);
    }

    public void HideAll()
    {
        if (!Player.Available) return;

        if (TryGetAddonByName<AtkUnitBase>("_Status", out var addon) && IsAddonReady(addon))
        {
            var validStatuses = Utils.GetMyStatusManager(Player.NameWithWorld).Statuses;
            UpdateStatus(addon, validStatuses, NumStatuses, true);
        }
    }

    private void OnStatusRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        var addon = (AtkUnitBase*)args.Addon;
        if (addon != null && IsAddonReady(addon) && P.CanModifyUI())
        {
            NumStatuses = 0;
            for (int i = 25; i >= 1; i--)
            {
                var c = addon->UldManager.NodeList[i];
                if (c->IsVisible)
                {
                    NumStatuses++;
                }
            }
        }
    }

    private void OnStatusUpdate(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        if (!Player.Available) return;
        if (!P.CanModifyUI()) return;
        var validStatuses = Utils.GetMyStatusManager(Player.NameWithWorld).Statuses;
        UpdateStatus((AtkUnitBase*)args.Addon, validStatuses, NumStatuses);
    }

    public void UpdateStatus(AtkUnitBase* addon, IEnumerable<MyStatus> statuses, int StatusCnt, bool hideAll = false)
    {
        if (addon != null && IsAddonReady(addon))
        {
            int baseCnt;
            if (P.CommonProcessor.NewMethod)
            {
                baseCnt = 25 - StatusCnt;
            }
            else
            {
                baseCnt = 25 - Player.Object.StatusList.Count(x => x.StatusId != 0);
                if (Svc.Condition[ConditionFlag.Mounted]) baseCnt--;
            }
            for (int i = baseCnt; i >= 1; i--)
            {
                var c = addon->UldManager.NodeList[i];
                if (c->IsVisible) c->NodeFlags ^= NodeFlags.Visible;
            }
            if (!hideAll)
            {
                foreach (var x in statuses)
                {
                    if (baseCnt < 1) break;
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
