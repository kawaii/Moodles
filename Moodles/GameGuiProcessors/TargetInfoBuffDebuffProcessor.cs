using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;

namespace Moodles.GameGuiProcessors;
public unsafe class TargetInfoBuffDebuffProcessor
{
    public int NumStatuses = 0;
    public TargetInfoBuffDebuffProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_TargetInfoBuffDebuff", OnTargetInfoBuffDebuffUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_TargetInfoBuffDebuff", OnTargetInfoBuffDebuffRequestedUpdate);
        if(Player.Available && TryGetAddonByName<AtkUnitBase>("_TargetInfoBuffDebuff", out var addon) && IsAddonReady(addon))
        {
            AddonRequestedUpdate(addon);
        }
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_TargetInfoBuffDebuff", OnTargetInfoBuffDebuffUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_TargetInfoBuffDebuff", OnTargetInfoBuffDebuffRequestedUpdate);
    }

    public void HideAll()
    {
        if(TryGetAddonByName<AtkUnitBase>("_TargetInfoBuffDebuff", out var addon) && IsAddonReady(addon))
        {
            UpdateAddon(addon, true);
        }
    }

    // Func helper to get around 7.4's internal AddonArgs while removing ArtificialAddonArgs usage
    private void OnTargetInfoBuffDebuffRequestedUpdate(AddonEvent t, AddonArgs args) => AddonRequestedUpdate((AtkUnitBase*)args.Addon.Address);
    
    private void AddonRequestedUpdate(AtkUnitBase* addonBase)
    {
        if (P == null) return;
        if (addonBase != null && IsAddonReady(addonBase))
        {
            NumStatuses = 0;
            for (var i = 3u; i <= 32; i++)
            {
                var c = addonBase->UldManager.SearchNodeById(i);
                if (c->IsVisible())
                {
                    NumStatuses++;
                }
            }
        }
        InternalLog.Verbose($"TargetInfo Requested update: {NumStatuses}");
    }

    private void OnTargetInfoBuffDebuffUpdate(AddonEvent type, AddonArgs args)
    {
        if(P == null) return;
        if(!Player.Available) return;
        if(!P.CanModifyUI()) return;
        UpdateAddon((AtkUnitBase*)args.Addon.Address);
    }

    // Didn't really know how to transfer to get the DalamudStatusList from here, so had to use IPlayerCharacter.
    public unsafe void UpdateAddon(AtkUnitBase* addon, bool hideAll = false)
    {
        var target = Svc.Targets.SoftTarget! ?? Svc.Targets.Target!;
        if(target is IPlayerCharacter pc)
        {
            if(addon != null && IsAddonReady(addon))
            {
                int baseCnt;
                if(P.CommonProcessor.NewMethod)
                {
                    baseCnt = 3 + NumStatuses;
                }
                else
                {
                    baseCnt = 3 + pc.StatusList.Count(x => x.StatusId != 0);
                }
                for(var i = baseCnt; i <= 32; i++)
                {
                    var c = addon->UldManager.SearchNodeById((uint)i);
                    if(c->IsVisible()) c->NodeFlags ^= NodeFlags.Visible;
                }
                if(!hideAll)
                {
                    var sm = ((Character*)pc.Address)->MyStatusManager();
                    foreach (var x in sm.Statuses)
                    {
                        if(baseCnt > 32) break;
                        var rem = x.ExpiresAt - Utils.Time;
                        if(rem > 0)
                        {
                            SetIcon(addon, baseCnt, x);
                            baseCnt++;
                        }
                    }
                }
            }
        }
    }

    private void SetIcon(AtkUnitBase* addon, int id, MyStatus status)
    {
        var container = addon->UldManager.SearchNodeById((uint)id);
        P.CommonProcessor.SetIcon(addon, container, status);
    }


}
