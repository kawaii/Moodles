using Moodles.Data;

namespace Moodles;

/// <summary> A Flag Enumerable that helps compress the information contained in the struct. </summary>
/// <remarks> Any future variables added to MyStatus as booleans can go into here as a flag. </remarks>
[Flags]
public enum MoodleFlags : byte
{
    None           = 0x00,
    Persistent     = 0x01, // Determines how application is handled.
    NoExpire       = 0x02, // labeled as "Permanent" in Moodles UI.
    AsPermanent    = 0x04, // labeled as "Sticky" in Moodles UI.
    Dispellable    = 0x08, // labeled as "Imply Dispellable" in Moodles UI.
    StackOnReapply = 0x10, // labeled as "Stack on Reapply" in Moodles UI.
}

/// <summary> A Record struct containing information nessisary to know during data transfer. </summary>
/// <remarks> Information only moodles should know is discarded. (Applier is considered excess & ignored) </remarks>
public readonly record struct MoodleStatus(
    Guid GUID,
    int IconID,
    string Title,
    string Description,
    string VfxPath,
    StatusType Type,
    long DurationTicks,
    long ExpiresAt,
    int Stacks,
    int StacksIncOnReapply,
    Guid DispellStatus,
    MoodleFlags Flags) : IComparable<MoodleStatus>
{
    public MoodleStatus()
        : this(Guid.Empty, 0, "", "", "", StatusType.Positive, 0, 0, 1, 1, Guid.Empty, MoodleFlags.None)
    { }

    public static readonly MoodleStatus Empty
        = new();

    public int CompareTo(MoodleStatus other) => GUID.CompareTo(other.GUID);
    public override int GetHashCode() => GUID.GetHashCode();
}

/// <summary> A Record struct containing information nessisary to know during data transfer. </summary>
public readonly record struct MoodlePreset(
    Guid GUID,
    string Title,
    IEnumerable<Guid> StatusList,
    PresetApplicationType Type) : IComparable<MoodlePreset>
{
    public MoodlePreset() : this(Guid.Empty, "", new List<Guid>(), PresetApplicationType.UpdateExisting)
    { }

    public static readonly MoodlePreset Empty
        = new();

    public int CompareTo(MoodlePreset other) => GUID.CompareTo(other.GUID);
    public override int GetHashCode() => GUID.GetHashCode();
}

/// <summar> Permissions Defined for a GSpeak Player to ensure consent is given when applying to another pair. </summary>
/// <param name="Pos"> Positive Moodle Statuses are allowed. </param>
/// <param name="Neg"> Negative Moodle Statuses are allowed. </param>
/// <param name="Special"> Special Moodle Statuses are allowed. </param>
/// <param name="PairMoodle"> This GSpeak Pair allows the application of your Statuses onto them. </param>
/// <param name="OwnMoodle"> This GSpeak Pair allows you to apply their Statuses to them. </param>
/// <param name="Permanent"> Permanent Statuses are allowed. </param>
/// <param name="Remove"> Status Removal is allowed. </param>
/// <param name="MaxTicks"> The Maximum time a Status can be applied in ticks. </param>
public readonly record struct GSpeakPerms(bool Pos, bool Neg, bool Special, bool PairMoodle, bool OwnMoodle, bool Permanent, bool Remove, long MaxTicks)
{
    public static GSpeakPerms Invalid => new(false, false, false, false, false, false, false, long.MinValue);
    public bool IsInvalid() => MaxTicks == long.MinValue;
}
// An Extension Class made to help extract additional information from the MoodleStatus struct.
// If you ever need the more precise information from the struct object for your plugins, copy it!
public static class IpcStructEx
{
    // More optimal than .HasFlag(), as it recreates the enum each call. This simply accesses the bit directly.
    public static bool IsPersistent(this MoodleStatus s)   => (s.Flags & MoodleFlags.Persistent) != 0;
    public static bool IsNoExpire(this MoodleStatus s)     => (s.Flags & MoodleFlags.NoExpire) != 0;
    public static bool IsPermanent(this MoodleStatus s)  => (s.Flags & MoodleFlags.AsPermanent) != 0;
    public static bool IsDispellable(this MoodleStatus s)  => (s.Flags & MoodleFlags.Dispellable) != 0;
    public static bool StacksOnReapply(this MoodleStatus s) => (s.Flags & MoodleFlags.StackOnReapply) != 0;

    public static MoodleFlags ToMoodleFlags(this MyStatus s)
        => (MoodleFlags)(
        (s.Persistent ? (int)MoodleFlags.Persistent : 0) |
        (s.NoExpire ? (int)MoodleFlags.NoExpire : 0) |
        (s.AsPermanent ? (int)MoodleFlags.AsPermanent : 0) |
        (s.Dispelable ? (int)MoodleFlags.Dispellable : 0) |
        (s.StackOnReapply ? (int)MoodleFlags.StackOnReapply : 0));

    // TotalDurationSeconds Converts to Ticks, name is missleading.
    public static MoodleStatus ToStatusStruct(this MyStatus s)
        => new MoodleStatus(s.GUID, s.IconID, s.Title, s.Description, s.CustomFXPath, s.Type, s.TotalDurationSeconds,
            s.ExpiresAt, s.Stacks, s.StacksIncOnReapply, s.StatusOnDispell, s.ToMoodleFlags());

    public static MoodlePreset ToPresetStruct(this Preset p)
        => new MoodlePreset(p.GUID, p.Title, p.Statuses, p.ApplicationType);

    public static MyStatus ToMyStatus(this MoodleStatus s)
    {
        return new MyStatus
        {
            GUID = s.GUID,
            IconID = s.IconID,
            Title = s.Title,
            Description = s.Description,
            ExpiresAt = s.ExpiresAt,
            Type = s.Type,
            Dispelable = s.IsDispellable(),
            Stacks = s.Stacks,
            StatusOnDispell = s.DispellStatus,
            CustomFXPath = s.VfxPath,
            StackOnReapply = s.StacksOnReapply(),
            StacksIncOnReapply = s.StacksIncOnReapply,
            Persistent = s.IsPersistent(),
            NoExpire = s.IsNoExpire(),
            AsPermanent = s.IsPermanent(),
        }.UpdateTime(s.DurationTicks);
    }

    public static MyStatus UpdateTime(this MyStatus s, long ticks)
    {
        var tSpan = TimeSpan.FromTicks(ticks);
        s.Days = tSpan.Days;
        s.Hours = tSpan.Hours;
        s.Minutes = tSpan.Minutes;
        s.Seconds = tSpan.Seconds;
        return s;
    }
}
