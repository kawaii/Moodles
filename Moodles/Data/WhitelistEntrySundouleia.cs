using System.Diagnostics.CodeAnalysis;

namespace Moodles.Data;

[Serializable]
public class WhitelistEntrySundouleia
{
    public WhitelistEntrySundouleia()
    {
        PlayerName = "Unknown-Sundouleia-Player";
        Address = nint.Zero;
        Access = MoodleAccess.None;
        MaxTime = TimeSpan.Zero;
        ClientAccess = MoodleAccess.None;
        ClientMaxTime = TimeSpan.Zero;
    }

    public WhitelistEntrySundouleia(nint address, string nameWithWorld, IPCMoodleAccessTuple accessTuple)
    {
        PlayerName = nameWithWorld;
        Address = address;
        UpdateData(accessTuple);
    }

    public string PlayerName;
    public nint Address;

    // Access the whitelist entry set for the Client.
    public MoodleAccess Access;
    public TimeSpan MaxTime;
    public long TotalMaxTime => (long)MaxTime.TotalMilliseconds;
    public long MaxExpireTimeUnix => Access.HasAny(MoodleAccess.Permanent) ? long.MaxValue : Utils.Time + TotalMaxTime;


    // Client Access for this whitelist entry.
    public MoodleAccess ClientAccess;
    public TimeSpan ClientMaxTime;
    public long ClientTotalMaxTime => (long)ClientMaxTime.TotalMilliseconds;
    public long ClientMaxExpireTimeUnix => ClientAccess.HasAny(MoodleAccess.Permanent) ? long.MaxValue : Utils.Time + ClientTotalMaxTime;

    public void UpdateData(IPCMoodleAccessTuple latestAccessTuple)
    {
        ClientAccess = latestAccessTuple.CallerAccess;
        Access = latestAccessTuple.OtherAccess;
        ClientMaxTime = TimeSpan.FromMilliseconds(latestAccessTuple.CallerMaxTime);
        MaxTime = TimeSpan.FromMilliseconds(latestAccessTuple.OtherMaxTime);
    }

    // Logic used to perform if a status is allowed to be applied to the entry by the client.
    public bool CanApplyStatus(MyStatus status, [NotNullWhen(false)] out string? errorTooltip)
    {
        errorTooltip = null;
        // Run checks for all access fields.
        if (!Access.HasAny(MoodleAccess.AllowOther))
            return (errorTooltip = $"{PlayerName} does not allow others to apply moodles.") is null;

        if (status.Type is StatusType.Positive && !Access.HasAny(MoodleAccess.Positive))
            return (errorTooltip = $"{PlayerName} does not accept positive moodles.") is null;

        if (status.Type is StatusType.Negative && !Access.HasAny(MoodleAccess.Negative))
            return (errorTooltip = $"{PlayerName} does not accept negative moodles.") is null;

        if (status.Type is StatusType.Special && !Access.HasAny(MoodleAccess.Special))
            return (errorTooltip = $"{PlayerName} does not accept special moodles.") is null;

        if (status.AsPermanent && !Access.HasAny(MoodleAccess.Permanent))
            return (errorTooltip = $"{PlayerName} prevents permanent moodles from being applied.") is null;

        if (status.ExpiresAt > ClientMaxExpireTimeUnix)
            return (errorTooltip = $"Moodle duration exceeds {PlayerName}'s maximum allowed time.") is null;

        return true;
    }
}
