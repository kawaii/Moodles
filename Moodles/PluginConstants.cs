using System;
using System.Numerics;

namespace Moodles;

public static class PluginConstants
{
    public const int MoodleMax = 20;
    public const int MaxTimerDepthSearch = 100;

                                          // seconds
    public const long MinSyncMoodleTicks = 30 * TimeSpan.TicksPerSecond;

    public const int PlayerSkeleton = 0;

    public const int Eos = -407;
    public const int Selene = -408;
    public const int EmeraldCarbuncle = -409;
    public const int RubyCarbuncle = -410;
    public const int Carbuncle = -411;
    public const int TopazCarbuncle = -412;
    public const int IfritEgi = -415;
    public const int TitanEgi = -416;
    public const int GarudaEgi = -417;
    public const int RookAutoTurret = -1027;
    public const int Bahamut = -1930;
    public const int AutomatonQueen = -2618;
    public const int Seraph = -2619;
    public const int Phoenix = -2620;
    public const int LivingShadow = -2621;
    public const int IffritII = -3122;
    public const int GarudaII = -3123;
    public const int TitanII = -3124;
    public const int SolarBahamut = -4038;

    public static readonly Vector2 JobIconSize = new Vector2(24f, 24f);
    public static readonly Vector2 StatusIconSize = new Vector2(24, 32);
}
