using ECommons.ChatMethods;
using Moodles.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles;
public class MyStatusManager
{
    public HashSet<Guid> AddTextShown = [];
    public HashSet<Guid> RemTextShown = [];
    public List<MyStatus> Statuses = [];

    public void AddOrUpdate(MyStatus newStatus)
    {
        for (int i = 0; i < Statuses.Count; i++)
        {
            if (Statuses[i].GUID == newStatus.GUID)
            {
                Statuses[i] = newStatus;
                return;
            }
        }
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
        Statuses.Add(newStatus);
    }

    public void Cancel(Guid id)
    {
        foreach (var stat in Statuses)
        {
            if(stat.GUID == id)
            {
                stat.ExpiresAt = 0;
            }
        }
    }

    public void Cancel(MyStatus myStatus) => Cancel(myStatus.GUID);

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
}
