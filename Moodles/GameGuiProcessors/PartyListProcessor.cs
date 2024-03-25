using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;
using Moodles.GameGuiProcessors;

namespace Moodles.Processors;
public unsafe class PartyListProcessor : IDisposable
{
    int[] NumStatuses = [0, 0, 0, 0, 0, 0, 0, 0];
    public PartyListProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_PartyList", OnPartyListUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListRequestedUpdate);
        if (Player.Available && TryGetAddonByName<AtkUnitBase>("_PartyList", out var addon) && IsAddonReady(addon))
        {
            this.OnPartyListRequestedUpdate(AddonEvent.PostRequestedUpdate, new ArtificialAddonArgs(addon));
        }
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "_PartyList", OnPartyListUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListRequestedUpdate);
    }

    public void HideAll()
    {
        if (TryGetAddonByName<AtkUnitBase>("_PartyList", out var addon) && IsAddonReady(addon))
        {
            this.UpdatePartyList(addon, true);
        }
    }

    private void OnPartyListRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        if (!Player.Available) return;
        var addon = (AtkUnitBase*)args.Addon;
        if (addon != null && IsAddonReady(addon) && P.CanModifyUI())
        {
            for (int i = 0; i < NumStatuses.Length; i++)
            {
                NumStatuses[i] = 0;
            }
            var index = 22;
            int storeIndex = 0;
            foreach (var player in GetVisibleParty())
            {
                //InternalLog.Verbose($"  Now checking {index} for {player}");
                if (player != null)
                {
                    var iconArray = Utils.GetNodeIconArray(addon->UldManager.NodeList[index]);
                    foreach(var x in iconArray)
                    {
                        if(x->IsVisible) NumStatuses[storeIndex]++;
                    }
                }
                storeIndex++;
                index--;
            }
        }
        InternalLog.Verbose($"PartyList Requested update: {NumStatuses.Print()}");
    }

    void OnPartyListUpdate(AddonEvent type, AddonArgs args)
    {
        if (P == null) return;
        UpdatePartyList((AtkUnitBase*)args.Addon);
    }

    public void UpdatePartyList(AtkUnitBase* addon, bool hideAll = false)
    {
        if (!Player.Available) return;
        if (!P.CanModifyUI()) return;
        if (addon != null && IsAddonReady(addon))
        {
            var partyMemberNodeIndex = 22;
            var party = GetVisibleParty();
            for (int n = 0; n < party.Count; n++)
            {
                var player = party[n];
                if (player != null)
                {
                    var iconArray = Utils.GetNodeIconArray(addon->UldManager.NodeList[partyMemberNodeIndex]);
                    //InternalLog.Information($"Icon array length for {player} is {iconArray.Length}");
                    for (int i = this.NumStatuses[n]; i < iconArray.Length; i++)
                    {
                        var c = iconArray[i];
                        if (c->IsVisible) c->NodeFlags ^= NodeFlags.Visible;
                    }
                    if (!hideAll)
                    {
                        int curIndex = this.NumStatuses[n];
                        foreach (var status in player.GetMyStatusManager().Statuses)
                        {
                            if (status.Type == StatusType.Special) continue;
                            if (curIndex >= iconArray.Length) break;
                            var rem = status.ExpiresAt - Utils.Time;
                            if (rem > 0)
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

    public List<PlayerCharacter> GetVisibleParty()
    {
        if (Svc.Party.Length < 2)
        {
            return [Svc.ClientState.LocalPlayer];
        }
        else
        {
            List<PlayerCharacter> ret = [Svc.ClientState.LocalPlayer];
            for (int i = 1; i < Math.Min(8, Svc.Party.Length); i++)
            {
                var obj = FakePronoun.Resolve($"<{i + 1}>");
                if (Svc.Objects.CreateObjectReference((nint)obj) is PlayerCharacter pc)
                {
                    ret.Add(pc);
                }
                else
                {
                    ret.Add(null);
                }
            }
            return ret;
        }
    }

    void SetIcon(AtkUnitBase* addon, AtkResNode* container, MyStatus status)
    {
        P.CommonProcessor.SetIcon(addon, container, status);
    }
}