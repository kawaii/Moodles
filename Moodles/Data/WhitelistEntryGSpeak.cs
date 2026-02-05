using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Diagnostics.CodeAnalysis;

namespace Moodles.Data;

[Serializable]
public class WhitelistEntryGSpeak
{
    // Does a weird thing where when loading from the config it loads this in for an initial entry, then loads everything else after.
    public WhitelistEntryGSpeak()
    {
        Name = "Unknown-GSpeak-Name";
        PlayerName = "Unknown-GSpeak-Player";
        Address = nint.Zero;
        Access = MoodleAccess.None;
        MaxTime = TimeSpan.Zero;
        ClientAccess = MoodleAccess.None;
        ClientMaxTime = TimeSpan.Zero;
    }

    public unsafe WhitelistEntryGSpeak(nint address, IPCMoodleAccessTuple accessTuple)
    {
        Name = ((Character*)address)->NameString;
        PlayerName = ((Character*)address)->GetNameWithWorld();
        Address = address;
        UpdateData(accessTuple);
    }

    // The Player Name associated with this whitelist entry.
    public string Name;
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

    public string CensoredName() => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => char.ToUpper(w[0]) + "."));

    public void UpdateData(IPCMoodleAccessTuple latest)
    {
        Access = (MoodleAccess)latest.RecipientAccessFlags;
        ClientAccess = (MoodleAccess)latest.ClientAccessFlags;

        MaxTime = TimeSpan.FromMilliseconds(latest.RecipientMaxTime);
        ClientMaxTime = TimeSpan.FromMilliseconds(latest.ClientMaxTime);
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
