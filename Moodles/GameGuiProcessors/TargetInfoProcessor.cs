using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;

namespace Moodles.GameGuiProcessors;
public unsafe class TargetInfoProcessor
{
    public int NumStatuses = 0;
    public TargetInfoProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_TargetInfo", OnTargetInfoUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_TargetInfo", OnTargetInfoRequestedUpdate);
        if(Player.Available && TryGetAddonByName<AtkUnitBase>("_TargetInfo", out var addon) && IsAddonReady(addon))
        {
            OnTargetInfoRequestedUpdate(AddonEvent.PostRequestedUpdate, new ArtificialAddonArgs(addon));
        }
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_TargetInfo", OnTargetInfoUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_TargetInfo", OnTargetInfoRequestedUpdate);
    }

    public void HideAll()
    {
        if(TryGetAddonByName<AtkUnitBase>("_TargetInfo", out var addon) && IsAddonReady(addon))
        {
            UpdateAddon(addon, true);
        }
    }

    private void OnTargetInfoRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if(P == null) return;
        var addon = (AtkUnitBase*)args.Addon.Address;
        if(addon != null && IsAddonReady(addon))
        {
            NumStatuses = 0;
            for(var i = 32; i >= 3; i--)
            {
                var c = addon->UldManager.NodeList[i];
                if(c->IsVisible())
                {
                    NumStatuses++;
                }
            }
        }
        InternalLog.Verbose($"TargetInfo Requested update: {NumStatuses}");
    }

    private void OnTargetInfoUpdate(AddonEvent type, AddonArgs args)
    {
        if(P == null) return;
        if(!Player.Available) return;
        if(!P.CanModifyUI()) return;
        UpdateAddon((AtkUnitBase*)args.Addon.Address);
    }

    public void UpdateAddon(AtkUnitBase* addon, bool hideAll = false)
    {
        var target = Svc.Targets.SoftTarget! ?? Svc.Targets.Target!;
        if(target is IPlayerCharacter pc)
        {
            if(addon != null && IsAddonReady(addon))
            {
                int baseCnt;
                if(P.CommonProcessor.NewMethod)
                {
                    baseCnt = 32 - NumStatuses;
                }
                else
                {
                    baseCnt = 32 - pc.StatusList.Count(x => x.StatusId != 0);
                }
                for(var i = baseCnt; i >= 3; i--)
                {
                    var c = addon->UldManager.NodeList[i];
                    if(c->IsVisible()) c->NodeFlags ^= NodeFlags.Visible;
                }
                if(!hideAll)
                {
                    var sm = ((Character*)pc.Address)->MyStatusManager();
                    foreach (var x in sm.Statuses)
                    {
                        if(baseCnt < 3) break;
                        var rem = x.ExpiresAt - Utils.Time;
                        if(rem > 0)
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
