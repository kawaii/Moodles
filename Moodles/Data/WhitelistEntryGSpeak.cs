namespace Moodles.Data;

[Serializable]
public class WhitelistEntryGSpeak
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

    public MoodlesGSpeakPairPerms ClientPermsForPair = new();

    internal long TotalMaxDurationSeconds => this.Seconds * 1000 + this.Minutes * 1000 * 60 + this.Hours * 1000 * 60 * 60 + this.Days * 1000 * 60 * 60 * 24;
    internal long MaxExpirationUnixTimeSeconds => this.AnyDuration ? long.MaxValue : Utils.Time + TotalMaxDurationSeconds;

    // checks if when applying incoming moodles, that we have the permission to apply them.
    public bool CheckStatus(MoodlesGSpeakPairPerms status, bool statusIsPerminant)
    {
        if (status.AllowPositive != ClientPermsForPair.AllowPositive) return false;
        if (status.AllowNegative != ClientPermsForPair.AllowNegative) return false;
        if (status.AllowSpecial != ClientPermsForPair.AllowSpecial) return false;
        if (status.AllowApplyingPairsMoodles != ClientPermsForPair.AllowApplyingPairsMoodles) return false;
        if (status.AllowPermanent != ClientPermsForPair.AllowPermanent) return false;
        if (status.MaxDuration > ClientPermsForPair.MaxDuration && !statusIsPerminant) return false;
        if (status.AllowRemoval != ClientPermsForPair.AllowRemoval) return false;
        return true;
    }

    public bool ArePermissionsDifferent(MoodlesGSpeakPairPerms newClientPermsForPair, MoodlesGSpeakPairPerms newPairPermsForClient)
    {
        return AllowedTypes.Contains(StatusType.Positive) != newPairPermsForClient.AllowPositive
        || AllowedTypes.Contains(StatusType.Negative) != newPairPermsForClient.AllowNegative
        || AllowedTypes.Contains(StatusType.Special) != newPairPermsForClient.AllowSpecial
        || CanApplyOurMoodles != newPairPermsForClient.AllowApplyingPairsMoodles // this flips because the intended direction is flipped
        || CanApplyTheirMoodles != newPairPermsForClient.AllowApplyingOwnMoodles // same as above
        || Days != newPairPermsForClient.MaxDuration.Days
        || Hours != newPairPermsForClient.MaxDuration.Hours
        || Minutes != newPairPermsForClient.MaxDuration.Minutes
        || Seconds != newPairPermsForClient.MaxDuration.Seconds
        || AnyDuration != newPairPermsForClient.AllowPermanent
        || CanRemoveMoodles != newPairPermsForClient.AllowRemoval
        || !ClientPermsForPair.Equals(newClientPermsForPair);
    }

    public void UpdatePermissions(MoodlesGSpeakPairPerms newPerms, MoodlesGSpeakPairPerms newPairPermsForClient)
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
        ClientPermsForPair = newPerms;
    }
}
