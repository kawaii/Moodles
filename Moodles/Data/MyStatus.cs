﻿using MemoryPack;

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
    public bool StackOnReapply = false;
    public int StacksIncOnReapply = 1;


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
        error = null;
        return true;
    }
}
