using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Moodles.Data;
using System.Windows.Forms;

namespace Moodles.Processors;
public unsafe class PartyListProcessor : IDisposable
{
    int[] NumStatuses = [0, 0, 0, 0, 0, 0, 0, 0];
    public PartyListProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_PartyList", OnPartyListUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListRequestedUpdate);
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
                if (player != null)
                {
                    for (int i = 5; i <= 14; i++)
                    {
                        var c = addon->UldManager.NodeList[index]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];
                        if (c->IsVisible) NumStatuses[storeIndex]++;
                    }
                }
                storeIndex++;
            }
        }
        InternalLog.Verbose($"PartyList Requested update: {NumStatuses.Print()}");
    }

    void OnPartyListUpdate(AddonEvent type, AddonArgs args)
    {
        UpdatePartyList((AtkUnitBase*)args.Addon);
    }

    public void UpdatePartyList(AtkUnitBase* addon, bool hideAll = false)
    {
        if (!Player.Available) return;
        if (!P.CanModifyUI()) return;
        if (addon != null && IsAddonReady(addon))
        {
            var index = 22;
            var party = GetVisibleParty();
            for (int n = 0; n < party.Count; n++)
            {
                var player = party[n];
                if (player != null)
                {
                    int baseCnt;
                    if (P.CommonProcessor.NewMethod)
                    {
                        baseCnt = NumStatuses[n] + 5;
                    }
                    else
                    {
                        baseCnt = player.StatusList.Count(x => x.StatusId != 0 && !P.CommonProcessor.SpecialStatuses.Contains(x.StatusId)) + 5;
                    }
                    for (int i = baseCnt; i <= 14; i++)
                    {
                        var c = addon->UldManager.NodeList[index]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];
                        if (c->IsVisible) c->NodeFlags ^= NodeFlags.Visible;
                    }
                    if (!hideAll)
                    {
                        foreach (var x in player.GetMyStatusManager().Statuses)
                        {
                            if (x.Type == StatusType.Special) continue;
                            if (baseCnt > 14) break;
                            var rem = x.ExpiresAt - Utils.Time;
                            if (rem > 0)
                            {
                                SetIcon(addon, baseCnt, x, index);
                                baseCnt++;
                            }
                        }
                    }
                    index--;
                }
            }
        }
    }

    List<PlayerCharacter> GetVisibleParty()
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

    void SetIcon(AtkUnitBase* addon, int index, MyStatus status, int partyIndex)
    {
        var container = addon->UldManager.NodeList[partyIndex]->GetAsAtkComponentNode()->Component->UldManager.NodeList[index];
        P.CommonProcessor.SetIcon(addon, container, status);
    }
}