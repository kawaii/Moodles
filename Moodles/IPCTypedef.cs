
global using MoodlesMoodleInfo = (System.Guid ID, uint IconID, string FullPath, string Title);
global using MoodlesProfileInfo = (System.Guid ID, string FullPath);

// Used for Tuple-Based IPC calls and associated data transfers.
global using MoodlesStatusInfo = (
    int Version,
    System.Guid GUID,
    int IconID,
    string Title,
    string Description,
    string CustomVFXPath,                   // What VFX to show on application.
    long ExpireTicks,                       // Permanent if -1, referred to as 'NoExpire' in MoodleStatus
    Moodles.Data.StatusType Type,           // Moodles StatusType enum.
    int Stacks,                             // Usually 1 when no stacks are used.
    int StackSteps,                         // How many stacks to add per reapplication.
    uint Modifiers,                         // What can be customized, casted to uint from Modifiers (Dalamud IPC Rules)
    System.Guid ChainedStatus,              // What status is chained to this one.
    Moodles.Data.ChainTrigger ChainTrigger, // What triggers the chained status.
    string Applier,                         // Who applied the moodle.
    string Dispeller,                       // When set, only this person can dispel your moodle.
    bool Permanent                          // Referred to as 'Sticky' in the Moodles UI
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


