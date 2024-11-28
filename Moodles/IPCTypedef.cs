
global using MoodlesGSpeakPairPerms = (
    bool AllowPositive,
    bool AllowNegative,
    bool AllowSpecial,
    bool AllowApplyingOwnMoodles,
    bool AllowApplyingPairsMoodles,
    System.TimeSpan MaxDuration,
    bool AllowPermanent,
    bool AllowRemoval
    );
global using MoodlesMoodleInfo = (System.Guid ID, uint IconID, string FullPath, string Title);
global using MoodlesProfileInfo = (System.Guid ID, string FullPath);
/// <summary>
/// Intended to be used for IPC to transfer full data without need for serialization
/// </summary>
global using MoodlesStatusInfo = (
    System.Guid GUID,
    int IconID,
    string Title,
    string Description,
    Moodles.Data.StatusType Type,
    string Applier,
    bool Dispelable,
    int Stacks,
    bool Persistent,
    int Days,
    int Hours,
    int Minutes,
    int Seconds,
    bool NoExpire,
    bool AsPermanent,
    System.Guid StatusOnDispell,
    string CustomVFXPath
);


