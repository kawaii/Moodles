namespace Moodles.Data;

[Serializable]
public class WhitelistEntryGSpeak
{
    public string PlayerName { get; init; } = "Unknown-Player";
    public GSpeakPerms ClientPermsForPair { get; private set; } = GSpeakPerms.Invalid; // Our perms for the pair.
    public GSpeakPerms PairPermsForClient { get; private set; } = GSpeakPerms.Invalid; // Pair Perms for us.
    public WhitelistEntryGSpeak()
    { }

    public WhitelistEntryGSpeak(string playerName, GSpeakPerms cpfp, GSpeakPerms ppfc)
    {
        PlayerName = playerName;
        ClientPermsForPair = cpfp;
        PairPermsForClient = ppfc;
    }

    // checks if when applying incoming moodles, that we have the permission to apply them.
    public bool StatusAllowed(MoodleStatus status)
    {
        if(!ClientPermsForPair.PairMoodle) return false;
        
        // Then check permissions.
        if(status.Type is StatusType.Positive && !ClientPermsForPair.Pos) return false;
        if(status.Type is StatusType.Negative && !ClientPermsForPair.Neg) return false;
        if(status.Type is StatusType.Special && !ClientPermsForPair.Special) return false;
        if(status.IsPermanent() && !ClientPermsForPair.Permanent) return false;
        if(status.DurationTicks > ClientPermsForPair.MaxTicks && !status.IsPermanent()) return false;
        return true;
    }

    public bool ArePermissionsDifferent(GSpeakPerms newCPFP, GSpeakPerms newPPFC)
        => !newCPFP.Equals(ClientPermsForPair) || !newPPFC.Equals(PairPermsForClient);

    public void UpdatePermissions(GSpeakPerms newCPFP, GSpeakPerms newPPFC)
        => (ClientPermsForPair, PairPermsForClient) = (newCPFP, newPPFC);
}
