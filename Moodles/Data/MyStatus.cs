using MemoryPack;
using System.Runtime.ConstrainedExecution;

namespace Moodles.Data;

// Updated MyStatus for desired new features holding updated structure.
[Serializable]
[MemoryPackable]
public partial class MyStatus
{
    internal string ID => GUID.ToString();

    // Essential
    public const int Version = 2;
    public Guid GUID = Guid.NewGuid();
    public int IconID;
    public string Title = "";
    public string Description = "";
    public string CustomFXPath = "";
    public long ExpiresAt;
    // Attributes
    public StatusType Type;
    public Modifiers Modifiers; // What can be customized with this moodle.
    public int Stacks = 1;
    public int StackSteps = 0; // How many stacks to add per reapplication.

    // Chaining Status (Applies when ChainTrigger condition is met)
    public Guid ChainedStatus = Guid.Empty;
    public ChainTrigger ChainTrigger;

    // Additional Behavior added overtime.
    public string Applier = "";
    public string Dispeller = ""; // Person who must be the one to dispel you.

    // Anything else that wants to be added here later that cant fit
    // into Modifiers or ChainTrigger can fit below cleanly.


    #region Conditional Serialization/Deserialization

    [MemoryPackIgnore] public bool Persistent = false;

    // Internals used to track data in the common processors.
    [NonSerialized] internal bool ApplyChain = false; // Informs processor to apply chain.
    [NonSerialized] internal bool ClickedOff = false; // Set when the status is right clicked off.
    [NonSerialized] internal int TooltipShown = -1;

    [MemoryPackIgnore] public int Days = 0;
    [MemoryPackIgnore] public int Hours = 0;
    [MemoryPackIgnore] public int Minutes = 0;
    [MemoryPackIgnore] public int Seconds = 0;
    [MemoryPackIgnore] public bool NoExpire = false;
    [MemoryPackIgnore] public bool AsPermanent = false;

    public bool ShouldSerializeGUID() => GUID != Guid.Empty;
    public bool ShouldSerializePersistent() => ShouldSerializeGUID();
    public bool ShouldSerializeExpiresAt() => ShouldSerializeGUID();

    #endregion Conditional Serialization/Deserialization

    internal uint AdjustedIconID => (uint)(IconID + Stacks - 1);
    internal long TotalDurationSeconds => Seconds * 1000 + Minutes * 1000 * 60 + Hours * 1000 * 60 * 60 + Days * 1000 * 60 * 60 * 24;

    public bool ShouldExpireOnChain() => ApplyChain && !Modifiers.Has(Modifiers.PersistAfterTrigger);
    public bool HadNaturalTimerFalloff() => ExpiresAt - Utils.Time <= 0 && !ApplyChain && !ClickedOff;

    public bool IsValid(out string error)
    {
        if(IconID == 0)
        {
            error = ("Icon is not set");
            return false;
        }
        if (IconID < 200000)
        {
            error = ("Icon is a Pre 7.1 Moodle!");
            return false;
        }
        if (Title.Length == 0)
        {
            error = ("Title is not set");
            return false;
        }
        if(TotalDurationSeconds < 1 && !NoExpire)
        {
            error = ("Duration is not set");
            return false;
        }
        {
            Utils.ParseBBSeString(Title, out var parseError);
            if(parseError != null)
            {
                error = $"Syntax error in title: {parseError}";
                return false;
            }
        }
        {
            Utils.ParseBBSeString(Description, out var parseError);
            if(parseError != null)
            {
                error = $"Syntax error in description: {parseError}";
                return false;
            }
        }
        error = null!;
        return true;
    }

    public MoodlesStatusInfo ToStatusTuple()
        => new MoodlesStatusInfo
        {
            Version = Version,
            GUID = GUID,
            IconID = IconID,
            Title = Title,
            Description = Description,
            CustomVFXPath = CustomFXPath,
            ExpireTicks = NoExpire ? -1 : TotalDurationSeconds,
            Type = Type,
            Stacks = Stacks,
            StackSteps = StackSteps,
            Modifiers = (uint)Modifiers,
            ChainedStatus = ChainedStatus,
            ChainTrigger = ChainTrigger,
            Applier = Applier,
            Dispeller = Dispeller,
            Permanent = AsPermanent
        };

    public static MyStatus FromTuple(MoodlesStatusInfo statusInfo)
    {
        var totalTime = statusInfo.ExpireTicks == -1 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(statusInfo.ExpireTicks);
        return new MyStatus
        {
            GUID = statusInfo.GUID,
            IconID = statusInfo.IconID,
            Title = statusInfo.Title,
            Description = statusInfo.Description,
            CustomFXPath = statusInfo.CustomVFXPath,

            Type = statusInfo.Type,
            Stacks = statusInfo.Stacks,
            StackSteps = statusInfo.StackSteps,
            Modifiers = (Modifiers)statusInfo.Modifiers,

            ChainedStatus = statusInfo.ChainedStatus,
            ChainTrigger = statusInfo.ChainTrigger,

            Applier = statusInfo.Applier,
            Dispeller = statusInfo.Dispeller,

            // Additional variables we can run assumptions on.
            Persistent = statusInfo.Permanent,
            Days = totalTime.Days,
            Hours = totalTime.Hours,
            Minutes = totalTime.Minutes,
            Seconds = totalTime.Seconds,
            NoExpire = statusInfo.ExpireTicks == -1,
        };
    }
}
