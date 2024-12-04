using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using MemoryPack;
using Moodles.Data;

namespace Moodles;
[Serializable]
public class MyStatusManager
{
    private static readonly MemoryPackSerializerOptions SerializerOptions = new()
    {
        StringEncoding = StringEncoding.Utf16,
    };
    public HashSet<Guid> AddTextShown = [];
    public HashSet<Guid> RemTextShown = [];
    public List<MyStatus> Statuses = [];
    public bool Ephemeral = false;
    internal IPlayerCharacter Owner => (IPlayerCharacter)Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == C.StatusManagers.FirstOrDefault(s => s.Value == this).Key);
    [NonSerialized] internal bool NeedFireEvent = false;

    public void AddOrUpdate(MyStatus newStatus, bool Unchecked = false, bool triggerEvent = true)
    {
        // Do not add null statuses
        if (!newStatus.IsNotNull())
        {
            PluginLog.Error($"Status {newStatus} was not added because it is null");
            return;
        }
        // Do not add statuses with invalid data
        if (!Unchecked)
        {
            if(!newStatus.IsValid(out var error))
            {
                Notify.Error(error);
                return;
            }
        }
        // check to see if the status is already present.
        for (var i = 0; i < Statuses.Count; i++)
        {
            if(Statuses[i].GUID == newStatus.GUID)
            {
                // use newStatus to check, in case we changed the setting between applications. Performs stack count updating.
                if (newStatus.StackOnReapply)
                {
                    var newStackCount = Statuses[i].Stacks;
                    // if this is valid, altar the newStatus stack count on reapplication to be current stack count + 1, if possible.
                    if (P.CommonProcessor.IconStackCounts.TryGetValue((uint)newStatus.IconID, out var maxStacks) && maxStacks > 1)
                    {
                        if (Statuses[i].Stacks + 1 <= maxStacks)
                        {
                            newStackCount++;
                            // remove status GUID from addTextShown so it can be shown again with the new stack on the next tick.
                            AddTextShown.Remove(newStatus.GUID);
                        }
                    }
                    // update stack count.
                    newStatus.Stacks = newStackCount;
                }
                // then update the status with the new status.
                Statuses[i] = newStatus;
                // fire trigger if needed and then early return.
                if(triggerEvent) NeedFireEvent = true;
                return;
            }
        }
        // if it was new, fire event if needed and add it.
        if (triggerEvent) NeedFireEvent = true;
        Statuses.Add(newStatus);
    }

    public void Cancel(Guid id, bool triggerEvent = true)
    {
        foreach(var stat in Statuses)
        {
            if(stat.GUID == id)
            {
                stat.ExpiresAt = 0;
                if(triggerEvent) NeedFireEvent = true;
            }
        }
    }

    public void Cancel(MyStatus myStatus, bool triggetEvent = true) => Cancel(myStatus.GUID, triggetEvent);

    public void ApplyPreset(Preset p)
    {
        var Ignore = Statuses.Where(x => x.Persistent).Select(x => x.GUID).ToList();
        if(p.ApplicationType == PresetApplicationType.ReplaceAll)
        {
            foreach(var x in Statuses)
            {
                if(!x.Persistent && !p.Statuses.Contains(x.GUID))
                {
                    //this.AddTextShown.Remove(x.GUID);
                    //x.GUID = Guid.NewGuid();
                    //this.AddTextShown.Add(x.GUID);
                    Cancel(x);
                }
            }
        }
        if(p.ApplicationType == PresetApplicationType.IgnoreExisting)
        {
            foreach(var x in Statuses)
            {
                Ignore.Add(x.GUID);
            }
        }
        foreach(var x in p.Statuses)
        {
            if(C.SavedStatuses.TryGetFirst(z => z.GUID == x, out var status))
            {
                if(!Ignore.Contains(status.GUID))
                {
                    AddOrUpdate(Utils.PrepareToApply(status));
                }
            }
        }
    }


    public void RemovePreset(Preset p)
    {
        foreach(var x in p.Statuses)
        {
            if(C.SavedStatuses.TryGetFirst(z => z.GUID == x, out var status))
            {
                Cancel(status);
            }
        }
    }

    public byte[] BinarySerialize()
    {
        return MemoryPackSerializer.Serialize(Statuses, SerializerOptions);
    }

    public string SerializeToBase64()
    {
        if(Statuses.Count == 0) return string.Empty;
        return Convert.ToBase64String(BinarySerialize());
    }

    public List<MoodlesStatusInfo> GetActiveStatusInfo()
    {
        if(Statuses.Count == 0) return [];
        return Statuses.Select(x => x.ToStatusInfoTuple()).ToList();
    }

    public void Apply(byte[] data) => SetStatusesAsEphemeral(MemoryPackSerializer.Deserialize<List<MyStatus>>(data));

    public void Apply(string base64string)
    {
        if(base64string.IsNullOrEmpty())
        {
            SetStatusesAsEphemeral(Array.Empty<MyStatus>());
        }
        else
        {
            Apply(Convert.FromBase64String(base64string));
        }
    }

    public void SetStatusesAsEphemeral(IEnumerable<MyStatus> newStatusList)
    {
        try
        {
            foreach(var x in Statuses)
            {
                if(!newStatusList.Any(n => n.GUID == x.GUID))
                {
                    x.ExpiresAt = 0;
                }
            }
            foreach(var x in newStatusList)
            {
                if(x.ExpiresAt > Utils.Time)
                {
                    AddOrUpdate(x, true, false);
                }
            }
            Ephemeral = true;
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public bool ContainsStatus(MyStatus status)
    {
        return ContainsStatus(status.GUID);
    }

    public bool ContainsStatus(Guid status)
    {
        var statusCount = Statuses.Count;
        for(var i = 0; i < statusCount; i++)
        {
            var curStatus = Statuses[i];
            if(curStatus.GUID == status)
            {
                return true;
            }
        }

        return false;
    }

    public bool ContainsPreset(Preset preset)
    {
        var statusCount = preset.Statuses.Count;
        for(var i = 0; i < statusCount; i++)
        {
            var statusGUID = preset.Statuses[i];
            if(!ContainsStatus(statusGUID))
            {
                return false;
            }
        }

        return true;
    }
}
