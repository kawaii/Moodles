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

    public void AddOrUpdate(MyStatus newStatus, UpdateSource source, bool Unchecked = false, bool triggerEvent = true)
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
            if (!newStatus.IsValid(out var error))
            {
                Notify.Error(error);
                return;
            }
        }
        // check to see if the status is already present.
        for (var i = 0; i < Statuses.Count; i++)
        {
            if (Statuses[i].GUID == newStatus.GUID)
            {
                // use newStatus to check, in case we changed the setting between applications. Performs stack count updating.
                if (newStatus.StackOnReapply)
                {
                    if (source is UpdateSource.StatusTuple)
                    {
                        // grab the current stack count.
                        var newStackCount = Statuses[i].Stacks;
                        // fetch what the max stack count for the icon is.
                        if (P.CommonProcessor.IconStackCounts.TryGetValue((uint)newStatus.IconID, out var max))
                        {
                            // if the stack count is less than the max, increase it by newStatus.StacksIncOnReapply.
                            // After, remove it from addTextShown to display the new stack.
                            if (Statuses[i].Stacks + newStatus.StacksIncOnReapply <= max)
                            {
                                newStackCount += newStatus.StacksIncOnReapply;
                                newStatus.Stacks = newStackCount;
                                AddTextShown.Remove(newStatus.GUID);
                            }
                        }
                    }
                    // Handle sources that are from status manager sets.
                    else if (source is UpdateSource.DataString)
                    {
                        // if the source is the data string, we simply apply the data string.
                        // HOWEVER, if and only if the stack count is different, we need to remove it from addTextShown to display the new stack.
                        if (Statuses[i].Stacks != newStatus.Stacks)
                            AddTextShown.Remove(newStatus.GUID);
                    }
                }
                // then update the status with the new status.
                Statuses[i] = newStatus;
                // fire trigger if needed and then early return.
                if (triggerEvent) NeedFireEvent = true;
                return;
            }
        }
        // if it was new, fire event if needed and add it.
        if (triggerEvent) NeedFireEvent = true;
        Statuses.Add(newStatus);
    }

    public void Cancel(Guid id, bool triggerEvent = true)
    {
        foreach (var stat in Statuses)
        {
            if (stat.GUID == id)
            {
                stat.ExpiresAt = 0;
                if (triggerEvent) NeedFireEvent = true;
            }
        }
    }

    public void Cancel(MyStatus myStatus, bool triggetEvent = true) => Cancel(myStatus.GUID, triggetEvent);

    public void ApplyPreset(Preset p)
    {
        var Ignore = Statuses.Where(x => x.Persistent).Select(x => x.GUID).ToList();
        if (p.ApplicationType == PresetApplicationType.ReplaceAll)
        {
            foreach (var x in Statuses)
            {
                if (!x.Persistent && !p.Statuses.Contains(x.GUID))
                {
                    //this.AddTextShown.Remove(x.GUID);
                    //x.GUID = Guid.NewGuid();
                    //this.AddTextShown.Add(x.GUID);
                    Cancel(x);
                }
            }
        }
        if (p.ApplicationType == PresetApplicationType.IgnoreExisting)
        {
            foreach (var x in Statuses)
            {
                Ignore.Add(x.GUID);
            }
        }
        foreach (var x in p.Statuses)
        {
            if (C.SavedStatuses.TryGetFirst(z => z.GUID == x, out var status))
            {
                if (!Ignore.Contains(status.GUID))
                {
                    AddOrUpdate(Utils.PrepareToApply(status), UpdateSource.StatusTuple);
                }
            }
        }
    }


    public void RemovePreset(Preset p)
    {
        foreach (var x in p.Statuses)
        {
            if (C.SavedStatuses.TryGetFirst(z => z.GUID == x, out var status))
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
        if (Statuses.Count == 0) return string.Empty;
        return Convert.ToBase64String(BinarySerialize());
    }

    public List<MoodleStatus> GetActiveStatusInfo()
    {
        if (Statuses.Count == 0) return [];
        return Statuses.Select(x => x.ToStatusStruct()).ToList();
    }

    public void Apply(byte[] data, UpdateSource source) => SetStatusesAsEphemeral(MemoryPackSerializer.Deserialize<List<MyStatus>>(data), source);

    public void Apply(string base64string, UpdateSource source = UpdateSource.DataString)
    {
        if (base64string.IsNullOrEmpty())
        {
            SetStatusesAsEphemeral(Array.Empty<MyStatus>(), source);
        }
        else
        {
            Apply(Convert.FromBase64String(base64string), source);
        }
    }

    public void SetStatusesAsEphemeral(IEnumerable<MyStatus> newStatusList, UpdateSource source)
    {
        try
        {
            foreach (var x in Statuses)
            {
                if (!newStatusList.Any(n => n.GUID == x.GUID))
                {
                    x.ExpiresAt = 0;
                }
            }
            foreach (var x in newStatusList)
            {
                if (x.ExpiresAt > Utils.Time)
                {
                    AddOrUpdate(x, source, true, false);
                }
            }
            Ephemeral = true;
        }
        catch (Exception e)
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
        for (var i = 0; i < statusCount; i++)
        {
            var curStatus = Statuses[i];
            if (curStatus.GUID == status)
            {
                return true;
            }
        }

        return false;
    }

    public bool ContainsPreset(Preset preset)
    {
        var statusCount = preset.Statuses.Count;
        for (var i = 0; i < statusCount; i++)
        {
            var statusGUID = preset.Statuses[i];
            if (!ContainsStatus(statusGUID))
            {
                return false;
            }
        }

        return true;
    }
}
