using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;
using Moodles.GameGuiProcessors;

namespace Moodles.Processors;
public unsafe class PartyListProcessor : IDisposable
{
    private int[] NumStatuses = [0, 0, 0, 0, 0, 0, 0, 0];
    public PartyListProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_PartyList", OnPartyListUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListRequestedUpdate);
        if(Player.Available && TryGetAddonByName<AtkUnitBase>("_PartyList", out var addon) && IsAddonReady(addon))
        {
            OnPartyListRequestedUpdate(AddonEvent.PostRequestedUpdate, new ArtificialAddonArgs(addon));
        }
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_PartyList", OnPartyListUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListRequestedUpdate);
    }

    public void HideAll()
    {
        if(TryGetAddonByName<AtkUnitBase>("_PartyList", out var addon) && IsAddonReady(addon))
        {
            UpdatePartyList(addon, true);
        }
    }

    private void OnPartyListRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if(P == null) return;
        if(!Player.Available) return;
        var addon = (AtkUnitBase*)args.Addon.Address;
        if(addon != null && IsAddonReady(addon) && P.CanModifyUI())
        {
            for(var i = 0; i < NumStatuses.Length; i++)
            {
                NumStatuses[i] = 0;
            }
            var index = 23;
            var storeIndex = 0;
            foreach(nint player in GetVisibleParty())
            {
                //InternalLog.Verbose($"  Now checking {index} for {player}");
                if(player != nint.Zero)
                {
                    var iconArray = Utils.GetNodeIconArray(addon->UldManager.NodeList[index]);
                    foreach(var x in iconArray)
                    {
                        if(x->IsVisible()) NumStatuses[storeIndex]++;
                    }
                }
                storeIndex++;
                index--;
            }
        }
        InternalLog.Verbose($"PartyList Requested update: {NumStatuses.Print()}");
    }

    private void OnPartyListUpdate(AddonEvent type, AddonArgs args)
    {
        if(P == null) return;
        UpdatePartyList((AtkUnitBase*)args.Addon.Address);
    }

    public void UpdatePartyList(AtkUnitBase* addon, bool hideAll = false)
    {
        if(!LocalPlayer.Available) return;
        if(!P.CanModifyUI()) return;
        
        if(addon != null && IsAddonReady(addon))
        {
            var partyMemberNodeIndex = 23;
            var party = GetVisibleParty();
            
            for(var n = 0; n < party.Count; n++)
            {
                var player = party[n];
                if(player != nint.Zero)
                {
                    var iconArray = Utils.GetNodeIconArray(addon->UldManager.NodeList[partyMemberNodeIndex]);
                    //InternalLog.Information($"Icon array length for {player} is {iconArray.Length}");
                    for(var i = NumStatuses[n]; i < iconArray.Length; i++)
                    {
                        var c = iconArray[i];
                        if(c->IsVisible()) c->NodeFlags ^= NodeFlags.Visible;
                    }
                    if(!hideAll)
                    {
                        var curIndex = NumStatuses[n];
                        foreach(var status in ((Character*)player)->MyStatusManager().Statuses)
                        {
                            if(status.Type == StatusType.Special) continue;
                            if(curIndex >= iconArray.Length) break;
                            
                            var rem = status.ExpiresAt - Utils.Time;
                            if(rem > 0)
                            {
                                SetIcon(addon, iconArray[curIndex], status);
                                curIndex++;
                            }
                        }
                    }
                }
                partyMemberNodeIndex--;
            }
        }
    }

    /// <summary>
    ///     Returns a list of pointer addresses that are Character* references for the visible party members.
    /// </summary>
    /// <returns></returns>
    public List<nint> GetVisibleParty()
    {
        if(Svc.Party.Length < 2)
        {
            return [LocalPlayer.Address];
        }
        else
        {
            List<nint> ret = [LocalPlayer.Address];
            for(var i = 1; i < Math.Min(8, Svc.Party.Length); i++)
            {
                var obj = ExtendedPronoun.Resolve($"<{i + 1}>");
                if (obj->IsCharacter())
                {
                    ret.Add((nint)obj);
                }
                else
                {
                    ret.Add(nint.Zero);
                }
            }
            return ret;
        }
    }

    private void SetIcon(AtkUnitBase* addon, AtkResNode* container, MyStatus status)
    {
        P.CommonProcessor.SetIcon(addon, container, status);
    }
}