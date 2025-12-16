using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using MemoryPack;
using Moodles.Data;
using static FFXIVClientStructs.FFXIV.Client.Game.StatusManager.Delegates;

namespace Moodles;
[Serializable]
public class MyStatusManager
{
    private static readonly MemoryPackSerializerOptions SerializerOptions = new()
    {
        StringEncoding = StringEncoding.Utf16,
    };
    // Changing anything in here will break everyones configs, so do not do that.
    public HashSet<Guid> AddTextShown = [];
    public HashSet<Guid> RemTextShown = [];
    public List<MyStatus> Statuses = [];
    public bool Ephemeral = false;

    /// <summary>
    ///     Beware this was originally a =>, so any location where it assumes valid nature should be double checked.
    /// </summary>
    [NonSerialized] internal unsafe Character* Owner = null!;
    [NonSerialized] internal bool NeedFireEvent = false;
    internal unsafe bool OwnerValid => Owner != null;

    public void Remove(MyStatus status, bool triggerEvent = true)
    {
        // If the status isn't in the Statuses, return.
        if (!Statuses.Remove(status))
            return;

        // Otherwise, since it removed successfully, remove the TextShowns.
        AddTextShown.Remove(status.GUID);
        RemTextShown.Remove(status.GUID);

        if (triggerEvent) NeedFireEvent = true;
    }

    /// <summary>
    ///     Primary pipeline that Moodle application runs though. <para />
    ///     Updates set through DataStrings should not process stack changes, 
    ///     as they are defining the stack count, where as new moodle or reapplied
    ///     one must handle stack count updates.
    /// </summary>
    public MyStatus? AddOrUpdate(MyStatus newStatus, UpdateSource source, bool Unchecked = false, bool triggerEvent = true)
    {
        // Do not add null statuses
        if (!newStatus.IsNotNull())
        {
            PluginLog.Error($"Status {newStatus} was not added because it is null");
            return null;
        }
        // Do not add statuses with invalid data
        if (!Unchecked)
        {
            if (!newStatus.IsValid(out var error))
            {
                Notify.Error(error);
                return null;
            }
        }

        for (var i = 0; i < Statuses.Count; i++)
        {
            if (Statuses[i].GUID == newStatus.GUID)
            {
                // Need to handle special logic for stack reapplication.
                if (newStatus.StackOnReapply)
                {
                    // If the source is a data string, we are only worried about setting the data.
                    if (source is UpdateSource.DataString)
                    {
                        if (Statuses[i].Stacks != newStatus.Stacks)
                            AddTextShown.Remove(newStatus.GUID);
                    }
                    // Otherwise, if a tuple and the status is stackable, handle the stack increase.
                    else if (source is UpdateSource.StatusTuple && P.CommonProcessor.IconStackCounts.TryGetValue((uint)newStatus.IconID, out var max))
                    {
                        PluginLog.Debug($"{Statuses[i].Title} can have {max} stacks max. (ref: {newStatus.Stacks}) (SM: {Statuses[i].Stacks})");
                        var curStacks = Statuses[i].Stacks;
                        // If the current + the increase is <= max, add it.
                        if (curStacks + newStatus.StacksIncOnReapply < max)
                        {
                            newStatus.Stacks = curStacks + newStatus.StacksIncOnReapply;
                            AddTextShown.Remove(newStatus.GUID);
                        }
                        // If already at max, just keep it at max, and avoid playing any effect.
                        else if (curStacks == max)
                        {
                            newStatus.Stacks = (int)max;
                        }
                        // If we increased the stacks to the point where we hit or went over the max, show the effect and set it to max.
                        else if (curStacks + newStatus.StacksIncOnReapply >= max)
                        {
                            newStatus.Stacks = (int)max;
                            AddTextShown.Remove(newStatus.GUID);
                        }
                    }
                }
                // Update the status.
                Statuses[i] = newStatus;
                // fire trigger if needed and then early return.
                if (triggerEvent) NeedFireEvent = true;
                return Statuses[i];
            }
        }
        // if it was new, fire event if needed and add it.
        if (triggerEvent) NeedFireEvent = true;
        Statuses.Add(newStatus);

        return newStatus;
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

    public List<MoodlesStatusInfo> GetActiveStatusInfo()
    {
        if (Statuses.Count == 0) return [];
        return Statuses.Select(x => x.ToStatusInfoTuple()).ToList();
    }

    public void Apply(byte[] data, UpdateSource source) => SetStatusesAsEphemeral(MemoryPackSerializer.Deserialize<List<MyStatus>>(data)!, source);

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
