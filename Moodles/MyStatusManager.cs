using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using MemoryPack;
using MemoryPack.Compression;
using Moodles.Data;
using System.IO.Compression;

namespace Moodles;
[Serializable]
public class MyStatusManager
{
    static readonly MemoryPackSerializerOptions SerializerOptions = new()
    {
        StringEncoding = StringEncoding.Utf16,
    };
    public HashSet<Guid> AddTextShown = [];
    public HashSet<Guid> RemTextShown = [];
    public List<MyStatus> Statuses = [];
    public bool Ephemeral = false;
    internal PlayerCharacter Owner => (PlayerCharacter)Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.GetNameWithWorld() == C.StatusManagers.FirstOrDefault(s => s.Value == this).Key);
    [NonSerialized] internal bool NeedFireEvent = false;

    public void AddOrUpdate(MyStatus newStatus, bool Unchecked = false, bool triggerEvent = true)
    {
        if (!newStatus.IsNotNull())
        {
            PluginLog.Error($"Status {newStatus} was not added because it is null");
            return;
        }
        if (!Unchecked)
        {
            if (!newStatus.IsValid(out var error))
            {
                Notify.Error(error);
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


    public void RemovePreset(Preset p)
    {
        foreach (var x in p.Statuses)
        {
            if (C.SavedStatuses.TryGetFirst(z => z.GUID == x, out var status))
            {
                this.Cancel(status);
            }
        }
    }

    public byte[] BinarySerialize()
    {
        return MemoryPackSerializer.Serialize(this.Statuses, SerializerOptions);
    }

    public string SerializeToBase64()
    {
        if (this.Statuses.Count == 0) return string.Empty;
        return Convert.ToBase64String(BinarySerialize());
    }

    public void Apply(byte[] data) => SetStatusesAsEphemeral(MemoryPackSerializer.Deserialize<List<MyStatus>>(data));

    public void Apply(string base64string)
    {
        if (base64string.IsNullOrEmpty())
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
            this.Ephemeral = true;
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
        var statusCount = this.Statuses.Count;
        for (var i = 0; i < statusCount; i++)
        {
            var curStatus = this.Statuses[i];
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
