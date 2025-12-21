using FFXIVClientStructs.FFXIV.Client.Game.Character;
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
    // Changing anything in here will break everyones configs, so do not do that.
    public HashSet<Guid> AddTextShown = [];
    public HashSet<Guid> RemTextShown = [];
    public List<MyStatus> Statuses = [];
    public bool Ephemeral = false;

    // Used by GSpeak, exclusive to the Client's StatusManager.
    // Helps prevent right-click off from working on these
    // statuses, preventing excessive IPC callback fighting.
    [NonSerialized] internal HashSet<Guid> LockedIds = [];

    [NonSerialized] internal unsafe Character* Owner = null!;
    [NonSerialized] internal bool NeedFireEvent = false;
    internal unsafe bool OwnerValid => Owner != null;

    // Handle locking logic.
    internal void LockStatuses(List<Guid> toLock) => LockedIds.UnionWith(toLock);

    internal void UnlockStatuses(List<Guid> toUnlock) => LockedIds.ExceptWith(toUnlock);

    internal void ClearLocks() => LockedIds.Clear();

    // Only controlled by the CommonProcessor and can bypass lock checks.
    public void Remove(MyStatus status, bool triggerEvent = true)
    {
        if (!Statuses.Remove(status)) return;

        AddTextShown.Remove(status.GUID);
        RemTextShown.Remove(status.GUID);

        if (triggerEvent) NeedFireEvent = true;
    }

    // Perform an add or update on statuses, ignoring lock validation.
    // Only performed from certain IPC calls.
    public MyStatus? AddOrUpdateLocked(MyStatus newStatus, bool triggerEvent = true)
    {
        if (!newStatus.IsNotNull())
        {
            PluginLog.Error($"Status {newStatus} was not added because it is null");
            return null;
        }

        for (var i = 0; i < Statuses.Count; i++)
        {
            // We are updating one, so we will ultimately return early.
            if (Statuses[i].GUID == newStatus.GUID)
            {
                CheckAndUpdateStacks(newStatus, Statuses[i], UpdateSource.StatusTuple);
                // If we want to persist time on reapplication, do so.
                if (Statuses[i].Modifiers.Has(Modifiers.PersistExpireTime))
                {
                    newStatus.ExpiresAt = Statuses[i].ExpiresAt;
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
        LockedIds.Add(newStatus.GUID);
        return newStatus;
    }

    public MyStatus? AddOrUpdate(MyStatus newStatus, UpdateSource source, bool Unchecked = false, bool triggerEvent = true)
    {
        // Fail additions or updates for statuses that are locked.
        if (LockedIds.Contains(newStatus.GUID)) return null;

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
            // We are updating one, so we will ultimately return early.
            if (Statuses[i].GUID == newStatus.GUID)
            {
                CheckAndUpdateStacks(newStatus, Statuses[i], source);
                // If we want to persist time on reapplication, do so.
                if (Statuses[i].Modifiers.Has(Modifiers.PersistExpireTime))
                {
                    newStatus.ExpiresAt = Statuses[i].ExpiresAt;
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

    private void CheckAndUpdateStacks(MyStatus newStatus, MyStatus existing, UpdateSource source)
    {
        if (!newStatus.Modifiers.Has(Modifiers.StacksIncrease)) return;

        // For DataStrings, simply remove the AddTextShown to ensure it displays with the latest stacks.
        if (source is UpdateSource.DataString)
        {
            if (existing.Stacks != newStatus.Stacks)
            {
                AddTextShown.Remove(newStatus.GUID);
            }
        }
        // Otherwise, for status tuples, perform all logic associated with stack increases.
        else if (source is UpdateSource.StatusTuple && P.CommonProcessor.IconStackCounts.TryGetValue((uint)newStatus.IconID, out var max))
        {
            UpdateStackLogic(newStatus, existing, (int)max);
        }
    }

    // Update stacks on the incoming status. Also handles any chain triggering from max stacks,
    // and any roll-over logic if set.
    private void UpdateStackLogic(MyStatus ns, MyStatus cur, int max)
    {
        var curStacks = cur.Stacks;
        // Current + Increase < max. (Just add it)
        if (curStacks + ns.StackSteps < max)
        {
            // Update stacks, ensure text will be shown.
            ns.Stacks = curStacks + ns.StackSteps;
            AddTextShown.Remove(ns.GUID);
        }
        // Current stacks are not max, but adding it will go over.
        else if (curStacks != max && curStacks + ns.StackSteps >= max)
        {
            // If the chain trigger is set and we want to do it on max stacks, update.
            if (cur.ChainedStatus != Guid.Empty && cur.ChainTrigger is ChainTrigger.HitMaxStacks)
            {
                // Set ApplyChain to true.
                ns.ApplyChain = true;
                ns.Stacks = curStacks;
            }
            else
            {
                ns.Stacks = cur.Modifiers.Has(Modifiers.StacksRollOver) 
                    ? Math.Clamp((curStacks + ns.StackSteps) - max, 1, max) : max;
                AddTextShown.Remove(ns.GUID);
            }
        }
        // We are already at max stacks, so do nothing.
        else
        {
            ns.Stacks = max;
        }
    }

    // Effectively 'remove'
    public void Cancel(Guid id, bool triggerEvent = true)
    {
        // If we are not allowed to remove the status, return.
        if (LockedIds.Contains(id)) return;

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

    // Any locked statuses will not be removed.
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
        return Statuses.Select(x => x.ToStatusTuple()).ToList();
    }

    public void Apply(byte[] data, UpdateSource source)
    {
        try
        {
            // Attempt to deserialize into the current format. If it fails, warn of old formatting.
            var statuses = MemoryPackSerializer.Deserialize<List<MyStatus>>(data, SerializerOptions);
            if (statuses != null)
            {
                SetStatusesAsEphemeral(statuses, source);
            }
            else
            {
                throw new Exception("Deserialized statuses were null");
            }
        }
        catch (Exception)
        {
            // Could add a failsafe for this maybe?
            PluginLog.Warning("A datastring was passed in with an old MyStatus format. Ignoring.");
        }
    }

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
