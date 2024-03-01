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
    //public string TextOverride = null;
    //public uint? TextColorOverride = null;
    //public uint? EdgeColorOverride = null;

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

    internal uint AdjustedIconID => (uint)(this.IconID + this.Stacks - 1);
    internal long TotalDurationSeconds => this.Seconds * 1000 + this.Minutes * 1000 * 60 + this.Hours * 1000 * 60 * 60 + this.Days * 1000 * 60 * 60 * 24;

    public bool IsValid(out string error)
    {
        if (this.IconID == 0)
        {
            error = ("Icon is not set");
            return false;
        }
        if (this.Title.Length == 0)
        {
            error = ("Title is not set");
            return false;
        }
        if (this.TotalDurationSeconds < 1 && !this.NoExpire)
        {
            error = ("Duration is not set");
            return false;
        }
        {
            Utils.ParseBBSeString(this.Title, out var parseError);
            if (parseError != null)
            {
                error = $"Syntax error in title: {parseError}";
                return false;
            }
        }
        {
            Utils.ParseBBSeString(this.Description, out var parseError);
            if (parseError != null)
            {
                error = $"Syntax error in description: {parseError}";
                return false;
            }
        }
        error = null;
        return true;
    }
}
