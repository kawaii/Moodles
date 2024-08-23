namespace Moodles.Data;

[Serializable]
public class WhitelistEntryMare
{
    public string PlayerName = "";
    public List<StatusType> AllowedTypes = [];

    public int Days = 0;
    public int Hours = 0;
    public int Minutes = 0;
    public int Seconds = 0;
    public bool AnyDuration = true;

    internal long TotalMaxDurationSeconds => Seconds * 1000 + Minutes * 1000 * 60 + Hours * 1000 * 60 * 60 + Days * 1000 * 60 * 60 * 24;
    internal long MaxExpirationUnixTimeSeconds => AnyDuration ? long.MaxValue : Utils.Time + TotalMaxDurationSeconds;

    public bool CheckStatus(MyStatus status)
    {
        if(!AllowedTypes.Contains(status.Type)) return false;
        if(status.ExpiresAt > MaxExpirationUnixTimeSeconds) return false;
        if(PlayerName != status.Applier) return false;
        return true;
    }
}
