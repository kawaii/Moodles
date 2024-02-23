using Dalamud.Game.Text.SeStringHandling;
using ECommons.ChatMethods;
using MessagePack;
using Moodles.OtterGuiHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.Data;
[Serializable]
[MessagePackObject]
public class MyStatus
{
    internal string ID => GUID.ToString();
    [Key(0)] public Guid GUID = Guid.NewGuid();
    [Key(1)] public int IconID;
    [Key(2)] public string Title = "";
    [Key(3)] public string Description = "";
    [Key(4)] public long ExpiresAt;
    [Key(5)] public StatusType Type;
    [Key(6)] public string Applier = "";
    [Key(7)] public bool Dispelable = false;
    [Key(8)] public int Stacks = 1;
    //public string TextOverride = null;
    //public uint? TextColorOverride = null;
    //public uint? EdgeColorOverride = null;

    [IgnoreMember] public bool Persistent = false;

    [NonSerialized] internal int TooltipShown = -1;

    [IgnoreMember] public int Days = 0;
    [IgnoreMember] public int Hours = 0;
    [IgnoreMember] public int Minutes = 0;
    [IgnoreMember] public int Seconds = 0;
    [IgnoreMember] public bool NoExpire = false;
    [IgnoreMember] public bool AsPermanent = false;

    public bool ShouldSerializeGUID() => GUID != Guid.Empty;
    public bool ShouldSerializePersistent() => ShouldSerializeGUID();
    public bool ShouldSerializeExpiresAt() => ShouldSerializeGUID();

    internal uint AdjustedIconID => (uint)(this.IconID + this.Stacks - 1);
    internal long TotalDurationSeconds => this.Seconds * 1000 + this.Minutes * 1000 * 60 + this.Hours * 1000 * 60 * 60 + this.Days * 1000 * 60 * 60 * 24;
}
