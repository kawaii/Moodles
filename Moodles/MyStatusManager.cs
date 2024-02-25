using MemoryPack;
using MemoryPack.Compression;
using Moodles.Data;
using System.IO.Compression;

namespace Moodles;
[Serializable]
public class MyStatusManager
{
    static MemoryPackSerializerOptions SerializerOptions = new()
    {
        StringEncoding = StringEncoding.Utf16,
    };
    public HashSet<Guid> AddTextShown = [];
    public HashSet<Guid> RemTextShown = [];
    public List<MyStatus> Statuses = [];
    [NonSerialized] internal bool NeedFireEvent = false;

    public void AddOrUpdate(MyStatus newStatus, bool Unchecked = false, bool triggerEvent = true)
    {
        if (!Unchecked)
        {
            if (newStatus.IconID == 0)
            {
                Notify.Error("Could not add status without icon");
                return;
            }
            if (newStatus.Title.Length == 0)
            {
                Notify.Error("Could not add status without title");
                return;
            }
            if (newStatus.TotalDurationSeconds < 1 && !newStatus.NoExpire)
            {
                Notify.Error("Could not add status without duration");
                return;
            }
        }
        for (int i = 0; i < Statuses.Count; i++)
        {
            if (Statuses[i].GUID == newStatus.GUID)
            {
                Statuses[i] = newStatus;
                if (triggerEvent) NeedFireEvent = true;
                return;
            }
        }
        if (triggerEvent) NeedFireEvent = true;
        Statuses.Add(newStatus);
    }

    public void Cancel(Guid id, bool triggerEvent = true)
    {
        foreach (var stat in Statuses)
        {
            if(stat.GUID == id)
            {
                stat.ExpiresAt = 0;
                if (triggerEvent) NeedFireEvent = true;
            }
        }
    }

    public void Cancel(MyStatus myStatus, bool triggetEvent = true) => Cancel(myStatus.GUID, triggetEvent);

    public void ApplyPreset(Preset p)
    {
        List<Guid> Ignore = this.Statuses.Where(x => x.Persistent).Select(x => x.GUID).ToList();
        if (p.ApplicationType == PresetApplicationType.ReplaceAll)
        {
            foreach (var x in this.Statuses)
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
        if(p.ApplicationType == PresetApplicationType.IgnoreExisting)
        {
            foreach (var x in this.Statuses)
            {
                Ignore.Add(x.GUID);
            }
        }
        foreach(var x in p.Statuses)
        {
            if(C.SavedStatuses.TryGetFirst(z => z.GUID == x, out var status))
            {
                if (!Ignore.Contains(status.GUID))
                {
                    this.AddOrUpdate(Utils.PrepareToApply(status));
                }
            }
        }
    }

    public byte[] BinarySerialize()
    {
        return MemoryPackSerializer.Serialize(this.Statuses, SerializerOptions);
    }

    public void DeserializeAndApply(byte[] data)
    {
        try
        {
            var newStatusList = MemoryPackSerializer.Deserialize<List<MyStatus>>(data);
            foreach(var x in this.Statuses)
            {
                if(!newStatusList.Any(n => n.GUID == x.GUID))
                {
                    x.ExpiresAt = 0;
                }
            }
            foreach(var x in newStatusList)
            {
                if (x.ExpiresAt > Utils.Time)
                {
                    this.AddOrUpdate(x, true, false);
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }
}
