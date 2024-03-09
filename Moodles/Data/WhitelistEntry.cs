using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.Data;
[Serializable]
public class WhitelistEntry
{
    public string PlayerName = "";
    public List<StatusType> AllowedTypes = [];

    public int Days = 0;
    public int Hours = 0;
    public int Minutes = 0;
    public int Seconds = 0;
    public bool AnyDuration = true;

    internal long TotalMaxDurationSeconds => this.Seconds * 1000 + this.Minutes * 1000 * 60 + this.Hours * 1000 * 60 * 60 + this.Days * 1000 * 60 * 60 * 24;
    internal long MaxExpirationUnixTimeSeconds => this.AnyDuration ? long.MaxValue : Utils.Time + TotalMaxDurationSeconds;

    public bool CheckStatus(MyStatus status)
    {
        if (!AllowedTypes.Contains(status.Type)) return false;
        if (status.ExpiresAt > MaxExpirationUnixTimeSeconds) return false;
        if (PlayerName != status.Applier) return false;
        return true;
    }
}
