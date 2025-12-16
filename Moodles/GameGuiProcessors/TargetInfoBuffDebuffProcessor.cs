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
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_TargetInfoBuffDebuff", TargetInfoBuffDebuffUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_TargetInfoBuffDebuff", TargetInfoBuffDebuffRequestedUpdate);
        if(Player.Available && TryGetAddonByName<AtkUnitBase>("_TargetInfoBuffDebuff", out var addon) && IsAddonReady(addon))
        {
            TargetInfoBuffDebuffRequestedUpdate(AddonEvent.PostRequestedUpdate, new ArtificialAddonArgs(addon));
        }
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_TargetInfoBuffDebuff", TargetInfoBuffDebuffUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_TargetInfoBuffDebuff", TargetInfoBuffDebuffRequestedUpdate);
    }

    public void HideAll()
    {
        if(TryGetAddonByName<AtkUnitBase>("_TargetInfoBuffDebuff", out var addon) && IsAddonReady(addon))
        {
            UpdateAddon(addon, true);
        }
    }

    private void TargetInfoBuffDebuffRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        var addon = (AtkUnitBase*)args.Addon.Address;
        if(addon != null && IsAddonReady(addon))
        {
            NumStatuses = 0;
            for(var i = 3u; i <= 32; i++)
            {
                var c = addon->UldManager.SearchNodeById(i);
                if(c->IsVisible())
                {
                    NumStatuses++;
                }
            }
        }
        InternalLog.Verbose($"TargetInfo Requested update: {NumStatuses}");
    }

    private void TargetInfoBuffDebuffUpdate(AddonEvent type, AddonArgs args)
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
