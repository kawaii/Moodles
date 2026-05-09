using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;

namespace Moodles.Processors;

public unsafe class PartyListProcessor : IDisposable
{
    private const int PartyUIIndex = 24;
    
    private int[] NumStatuses = [0, 0, 0, 0, 0, 0, 0, 0];
    
    public PartyListProcessor()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate,          "_PartyList", OnPartyListUpdate);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnAlcPartyListRequestedUpdate);
        
        if (!LocalPlayer.Available)
        {
            return;
        }
        
        if (!TryGetAddonByName("_PartyList", out AtkUnitBase* addon))
        {
            return;
        }
        
        if (!IsAddonReady(addon))
        {
            return;
        }
        

        AddonRequestedUpdate(addon);
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate,          "_PartyList", OnPartyListUpdate);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnAlcPartyListRequestedUpdate);
    }

    public void HideAll()
    {
        if (!TryGetAddonByName<AtkUnitBase>("_PartyList", out AtkUnitBase* addon) && IsAddonReady(addon))
        {
            return;
        }
        
        UpdatePartyList(addon, true);
    }

    // Func helper to get around 7.4's internal AddonArgs while removing ArtificialAddonArgs usage 
    private void OnAlcPartyListRequestedUpdate(AddonEvent t, AddonArgs args) 
        => AddonRequestedUpdate((AtkUnitBase*)args.Addon.Address);

    private void OnPartyListUpdate(AddonEvent type, AddonArgs args) 
        => UpdatePartyList((AtkUnitBase*)args.Addon.Address);
    
    private void ClearNumStatuses()
    {
        for (var i = 0; i < NumStatuses.Length; i++)
        {
            NumStatuses[i] = 0;
        }
    }

    private void AddonRequestedUpdate(AtkUnitBase* addonBase)
    {
        if (addonBase == null)
        {
            return;
        }
        
        if (!LocalPlayer.Available)
        {
            return;
        }
        
        if (!IsAddonReady(addonBase))
        {
            return;
        }

        if (!P.CanModifyUI())
        {
            return;
        }
        
        ClearNumStatuses();
        
        int index      = PartyUIIndex;
        int storeIndex = 0;
        
        foreach (nint player in GetVisibleParty())
        {
            if (player != nint.Zero)
            {
                AtkResNode*[] iconArray = Utils.GetNodeIconArray(addonBase->UldManager.NodeList[index]);
                
                foreach (var x in iconArray)
                {
                    if (!x->IsVisible())
                    {
                        continue;
                    }
                    
                    NumStatuses[storeIndex]++;
                }
            }
            
            storeIndex++;
            index--;
        }

        InternalLog.Verbose($"PartyList Requested update: {NumStatuses.Print()}");
    }

    public void UpdatePartyList(AtkUnitBase* addon, bool hideAll = false)
    {
        if (!LocalPlayer.Available)
        {
            return;
        }
        
        if (!P.CanModifyUI())
        {
            return;
        }

        if (addon == null)
        {
            return;
        }
        
        if (!IsAddonReady(addon))
        {
            return;
        }
        
        nint[] party = GetVisibleParty();

        for (int n = 0; n < party.Length; n++)
        {
            int partyMemberNodeIndex = PartyUIIndex - n;
            
            nint player = party[n];
            
            if (player == nint.Zero)
            {
                continue;
            }
            
            AtkResNode*[] iconArray = Utils.GetNodeIconArray(addon->UldManager.NodeList[partyMemberNodeIndex]);
            
            for (int i = NumStatuses[n]; i < iconArray.Length; i++)
            {
                AtkResNode* c = iconArray[i];
                
                if (!c->IsVisible())
                {
                    continue;
                }
                
                c->NodeFlags ^= NodeFlags.Visible;
            }
            
            if (hideAll)
            {
                continue;
            }
            
            int curIndex = NumStatuses[n];
            
            foreach (MyStatus status in ((Character*)player)->MyStatusManager().Statuses)
            {
                if (status.Type == StatusType.Special)
                {
                    continue;
                }
                
                if (curIndex >= iconArray.Length)
                {
                    break;
                }

                long rem = status.ExpiresAt - Utils.Time;
                
                if (rem <= 0)
                {
                    continue;
                }
                
                SetIcon(addon, iconArray[curIndex], status);
                
                curIndex++;
            }
        }
    }

    public nint[] GetVisibleParty()
    {
        nint[] partyAddresses = [0, 0, 0, 0, 0, 0, 0, 0];
        
        for (int i = 0; i < PartyListNumberArray.Instance()->PartyListCount; i++)
        {
            // Its actually entityId, I PR´d a fix to ClientStructs already.
            uint entityId = (uint)PartyListNumberArray.Instance()->PartyMembers[i].ContentId;
            
            nint address = nint.Zero;
            
            foreach (BattleChara* bChara in CharacterManager.Instance()->BattleCharas)
            {
                if (bChara == null)
                {
                    continue;
                }
                
                if (bChara->EntityId != entityId)
                {
                    continue;
                }
                
                address = (nint)bChara;
            }
            
            partyAddresses[i] = address;
        }
        
        return partyAddresses;
    }
    
    private void SetIcon(AtkUnitBase* addon, AtkResNode* container, MyStatus status) 
        => P.CommonProcessor.SetIcon(addon, container, status);
}