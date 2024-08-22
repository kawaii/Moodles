namespace Moodles.Data;

[Serializable]
public class WhitelistEntryGSpeak
{
    // name of the pair that is visible and added to GSpeak
    public string PlayerName = "Unknown-Player";
    // Allow Positive
    // Allow Negative
    // Allow Special
    // Allow Applying Pairs Moodles (pair can apply moodles to you)
    // Allow Applying Own Moodles (pair can apply your moodles)
    // Max Duration (max duration allowed when effect is not permanent)
    // Allow Permanent (pair can apply permanent moodles)
    // Allow Removal (pair can remove your moodles)
    public MoodlesGSpeakPairPerms ClientPermsForPair = new();

    public MoodlesGSpeakPairPerms PairPermsForClient = new();

    internal long TotalMaxDurationSecondsClient => this.ClientPermsForPair.MaxDuration.Seconds * 1000 + this.ClientPermsForPair.MaxDuration.Minutes * 1000 * 60
                                                 + this.ClientPermsForPair.MaxDuration.Hours * 1000 * 60 * 60 + this.ClientPermsForPair.MaxDuration.Days * 1000 * 60 * 60 * 24;
    internal long MaxExpirationUnixTimeSecondsClient => this.ClientPermsForPair.AllowPermanent ? long.MaxValue : Utils.Time + TotalMaxDurationSecondsClient;

    internal long TotalMaxDurationSecondsPair => this.PairPermsForClient.MaxDuration.Seconds * 1000 + this.PairPermsForClient.MaxDuration.Minutes * 1000 * 60
                                               + this.PairPermsForClient.MaxDuration.Hours * 1000 * 60 * 60 + this.PairPermsForClient.MaxDuration.Days * 1000 * 60 * 60 * 24;
    internal long MaxExpirationUnixTimeSecondsPair => this.PairPermsForClient.AllowPermanent ? long.MaxValue : Utils.Time + TotalMaxDurationSecondsPair;

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
        => !newClientPermsForPair.Equals(ClientPermsForPair) || !newPairPermsForClient.Equals(PairPermsForClient);

    public void UpdatePermissions(MoodlesGSpeakPairPerms newPerms, MoodlesGSpeakPairPerms newPairPermsForClient)
    {
        ClientPermsForPair = newPerms;
        PairPermsForClient = newPairPermsForClient;
    }
}
