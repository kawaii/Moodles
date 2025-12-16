
global using MoodlesMoodleInfo = (System.Guid ID, uint IconID, string FullPath, string Title);
global using MoodlesProfileInfo = (System.Guid ID, string FullPath);

// Used for Tuple-Based IPC calls and associated data transfers.
global using MoodlesStatusInfo = (
    System.Guid GUID,
    int IconID,
    string Title,
    string Description,
    Moodles.Data.StatusType Type,   // Moodles StatusType enum, as a byte.
    string CustomVFXPath,           // What VFX to show on application.
    int Stacks,                     // Usually 1 when no stacks are used.
    long ExpireTicks,               // Permanent if -1, referred to as 'NoExpire' in MoodleStatus
    string Applier,                 // Who applied the moodle. (Only relevant when updating active moodles)
    bool Dispelable,                // Can be dispelled by others.
    string Dispeller,               // When set, only this person can dispel your moodle.
    bool Permanent,                 // Referred to as 'Sticky' in the Moodles UI
    System.Guid StatusOnDispell,    // What status is applied upon the moodle being right-clicked off.
    bool ReapplyIncStacks,          // If stacks increase on reapplication.
    int StackIncCount,              // How many stacks get added on each reapplication.
    bool UseStacksOnDispelStatus    // If dispelling transfers stacks to the dispel-applied moodle.
);

global using MoodlePresetInfo = (
    System.Guid GUID,
    System.Collections.Generic.List<System.Guid> Statuses,
    Moodles.Data.PresetApplicationType ApplicationType,
    string Title
);

// The IPC Tuple used to define MoodleAccess permission between recipient and client.
// Note that this should be using a MoodleAccess Flag enum, but dalamud's Newtonsoft parsing does not play
// nice with [Flag] Enums, especially in tuples.
global using IPCMoodleAccessTuple = (short OtherAccessFlags, long OtherMaxTime, short CallerAccessFlags, long CallerMaxTime);


