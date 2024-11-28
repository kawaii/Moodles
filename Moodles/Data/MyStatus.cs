using MemoryPack;

namespace Moodles.Data;
[Serializable]
[MemoryPackable]
public partial class MyStatus
{
    internal string ID => GUID.ToString();
    public Guid GUID = Guid.NewGuid();
    public int IconID;
    public string Title = "";
    public string Description = "";
    public long ExpiresAt;
    public StatusType Type;
    public string Applier = "";
    public bool Dispelable = false;
    public int Stacks = 1;
    public Guid StatusOnDispell = Guid.Empty;
    public string CustomFXPath = "";

    [MemoryPackIgnore] public bool Persistent = false;

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

    internal uint AdjustedIconID => (uint)(IconID + Stacks - 1);
    internal long TotalDurationSeconds => Seconds * 1000 + Minutes * 1000 * 60 + Hours * 1000 * 60 * 60 + Days * 1000 * 60 * 60 * 24;

    public bool IsValid(out string error)
    {
        if(IconID == 0)
        {
            error = ("Icon is not set");
            return false;
        }
        if(Title.Length == 0)
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
        error = null;
        return true;
    }

    public MoodlesStatusInfo ToStatusInfoTuple()
        => (GUID, IconID, Title, Description, Type, Applier, Dispelable, Stacks, Persistent, Days, Hours, Minutes, Seconds, NoExpire, AsPermanent, StatusOnDispell, CustomFXPath);

    public static MyStatus FromStatusInfoTuple(MoodlesStatusInfo statusInfo)
    {
        return new MyStatus
        {
            GUID = statusInfo.GUID,
            IconID = statusInfo.IconID,
            Title = statusInfo.Title,
            Description = statusInfo.Description,
            Type = statusInfo.Type,
            Applier = statusInfo.Applier,
            Dispelable = statusInfo.Dispelable,
            Stacks = statusInfo.Stacks,
            Persistent = statusInfo.Persistent,
            Days = statusInfo.Days,
            Hours = statusInfo.Hours,
            Minutes = statusInfo.Minutes,
            Seconds = statusInfo.Seconds,
            NoExpire = statusInfo.NoExpire,
            AsPermanent = statusInfo.AsPermanent,
            StatusOnDispell = statusInfo.StatusOnDispell,
            CustomFXPath = statusInfo.CustomVFXPath
        };
    }
}
