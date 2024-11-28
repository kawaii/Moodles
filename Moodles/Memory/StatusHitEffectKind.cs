using System.ComponentModel;

namespace Moodles;
public enum StatusHitEffectKind : short
{
    Spark_Blue = 31,
    Spark_Pink = 32,
    Spark_Yellow = 33,
    Burst_Orange = 37,
    Bubbles_Purple = 41,
    Bubbles_White = 42,
    Electro_Orange = 43,
    Hearts = 45,
    Electro_Yellow = 46,
    Feet_Portal = 51,
    Enhancement = 60,
    [Description("dk05th_stdn0t")]
    Enfeeblement = 61,
    [Description("dk04ht_canc0h")]
    FadeBuff = 73,
    Fog_Explosion = 76,
    Notes_Feet = 77,
}
