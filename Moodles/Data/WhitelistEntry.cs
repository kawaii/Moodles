using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.Data;
[Serializable]
public class WhitelistEntry
{
    // name of the pair that is visible and added to GSpeak
    public string PlayerName = "Unknown-Player";
    // the types of permissions they allow our client to apply to them.
    public List<StatusType> AllowedTypes = [];

    // the max moodle duration intervals they allow our client to apply to them.
    public int Days = 0;
    public int Hours = 0;
    public int Minutes = 0;
    public int Seconds = 0;
    
    // if they allow permanent moodles or not.
    public bool AnyDuration = true;

    // if they allow us to apply our moodles to their client.
    public bool CanApplyOurMoodles = false;

    // if they allow us to apply their moodles to them.
    public bool CanApplyTheirMoodles = false;

    // if we can remove active moodles off them.
    public bool CanRemoveMoodles = false;

    internal long TotalMaxDurationSeconds => this.Seconds * 1000 + this.Minutes * 1000 * 60 + this.Hours * 1000 * 60 * 60 + this.Days * 1000 * 60 * 60 * 24;
    internal long MaxExpirationUnixTimeSeconds => this.AnyDuration ? long.MaxValue : Utils.Time + TotalMaxDurationSeconds;

    public bool CheckStatus(MyStatus status)
    {
        if (!AllowedTypes.Contains(status.Type)) return false;
        if (status.ExpiresAt > MaxExpirationUnixTimeSeconds) return false;
        if (PlayerName != status.Applier) return false;
        return true;
    }

    public bool ArePermissionsDifferent(OtherPairsMoodlePermsForClient newPerms)
    {
        return AllowedTypes.Contains(StatusType.Positive) != newPerms.AllowPositive
        || AllowedTypes.Contains(StatusType.Negative) != newPerms.AllowNegative
        || AllowedTypes.Contains(StatusType.Special) != newPerms.AllowSpecial
        || CanApplyOurMoodles != newPerms.AllowApplyingPairsMoodles // this flips because the intended direction is flipped
        || CanApplyTheirMoodles != newPerms.AllowApplyingOwnMoodles // same as above
        || Days != newPerms.MaxDuration.Days
        || Hours != newPerms.MaxDuration.Hours
        || Minutes != newPerms.MaxDuration.Minutes
        || Seconds != newPerms.MaxDuration.Seconds
        || AnyDuration != newPerms.AllowPermanent
        || CanRemoveMoodles != newPerms.AllowRemoval;
    }

    public void UpdatePermissions(OtherPairsMoodlePermsForClient newPerms)
    {
        AllowedTypes = new List<StatusType>();
        if (newPerms.AllowPositive) AllowedTypes.Add(StatusType.Positive);
        if (newPerms.AllowNegative) AllowedTypes.Add(StatusType.Negative);
        if (newPerms.AllowSpecial) AllowedTypes.Add(StatusType.Special);
        CanApplyOurMoodles = newPerms.AllowApplyingPairsMoodles;
        CanApplyTheirMoodles = newPerms.AllowApplyingOwnMoodles;
        Days = newPerms.MaxDuration.Days;
        Hours = newPerms.MaxDuration.Hours;
        Minutes = newPerms.MaxDuration.Minutes;
        Seconds = newPerms.MaxDuration.Seconds;
        AnyDuration = newPerms.AllowPermanent;
        CanRemoveMoodles = newPerms.AllowRemoval;
    }
}
