using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;

namespace Moodles.GameGuiProcessors;
public unsafe class FocusTargetInfoProcessor
{
    private int NumStatuses = 0;

    public FocusTargetInfoProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_FocusTargetInfo", OnFocusTargetInfoUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_FocusTargetInfo", OnFocusTargetInfoRequestedUpdate);
        if(LocalPlayer.Available && TryGetAddonByName<AtkUnitBase>("_FocusTargetInfo", out var addon) && IsAddonReady(addon))
        {
            AddonRequestedUpdate(addon);
        }
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_FocusTargetInfo", OnFocusTargetInfoUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_FocusTargetInfo", OnFocusTargetInfoRequestedUpdate);
    }

    public void HideAll()
    {
        if(TryGetAddonByName<AtkUnitBase>("_FocusTargetInfo", out var addon) && IsAddonReady(addon))
        {
            UpdateAddon(addon, true);
        }
    }

    // Func helper to get around 7.4's internal AddonArgs while removing ArtificialAddonArgs usage 
    private void OnFocusTargetInfoRequestedUpdate(AddonEvent t, AddonArgs args) => AddonRequestedUpdate((AtkUnitBase*)args.Addon.Address);

    private void OnFocusTargetInfoUpdate(AddonEvent type, AddonArgs args)
    {
        if(P == null) return;
        if(!LocalPlayer.Available) return;
        if(P.CanModifyUI())
        {
            UpdateAddon((AtkUnitBase*)args.Addon.Address);
        }
    }

    private void AddonRequestedUpdate(AtkUnitBase* addonBase)
    {
        if (P == null) return;
        if (addonBase != null && IsAddonReady(addonBase))
        {
            NumStatuses = 0;
            for (var i = 8; i >= 4; i--)
            {
                var c = addonBase->UldManager.NodeList[i];
                if (c->IsVisible())
                {
                    NumStatuses++;
                }
            }
        }
        InternalLog.Verbose($"FocusTarget Requested update: {NumStatuses}");
    }

    public void UpdateAddon(AtkUnitBase* addon, bool hideAll = false)
    {
        if(addon != null && IsAddonReady(addon) && Svc.Targets.FocusTarget is IPlayerCharacter pc)
        {
            int baseCnt;
            if(P.CommonProcessor.NewMethod)
            {
                baseCnt = 8 - NumStatuses;
            }
            else
            {
                baseCnt = 8 - LocalPlayer.StatusList.Count(x => x.StatusId != 0 && !P.CommonProcessor.SpecialStatuses.Contains(x.StatusId));
            }
            for(var i = baseCnt; i >= 4; i--)
            {
                var c = addon->UldManager.NodeList[i];
                if(c->IsVisible()) c->NodeFlags ^= NodeFlags.Visible;
            }
            if(!hideAll)
            {
                unsafe
                {
                    var sm = ((Character*)pc.Address)->MyStatusManager();
                    foreach (var x in sm.Statuses)
                    {
                        if (x.Type == StatusType.Special) continue;
                        if (baseCnt < 4) break;
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
    }

    private void SetIcon(AtkUnitBase* addon, int index, MyStatus status)
    {
        var container = addon->UldManager.NodeList[index];
        P.CommonProcessor.SetIcon(addon, container, status);
    }
}
