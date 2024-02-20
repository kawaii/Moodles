using Dalamud.Game.Text.SeStringHandling;
using ECommons.ChatMethods;
using Moodles.OtterGuiHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.Data;
[Serializable]
public class MyStatus
{
    internal string ID => GUID.ToString();
    public Guid GUID = Guid.NewGuid();
    public int IconID;
    public string Title = "";
    public string Description = "";
    public long ExpiresAt;
    public StatusType Type;
    public string Applier = "";
    public string TextOverride = null;
    public uint? TextColorOverride = null;
    public uint? EdgeColorOverride = null;
    public bool Dispelable = false;
    public int Stacks = 1;

    public bool Persistent = false;

    [NonSerialized] internal int TooltipShown = -1;

    public int Days = 0;
    public int Hours = 0;
    public int Minutes = 0;
    public int Seconds = 0;
    public bool NoExpire = false;
    public bool AsPermanent = false;

    public bool ShouldSerializeGUID() => GUID != Guid.Empty;
    public bool ShouldSerializePersistent() => ShouldSerializeGUID();
    public bool ShouldSerializeExpiresAt() => ShouldSerializeGUID();

    internal uint AdjustedIconID => (uint)(this.IconID + this.Stacks - 1);
    internal long TotalDurationSeconds => this.Seconds * 1000 + this.Minutes * 1000 * 60 + this.Hours * 1000 * 60 * 60 + this.Days * 1000 * 60 * 60 * 24;
}
