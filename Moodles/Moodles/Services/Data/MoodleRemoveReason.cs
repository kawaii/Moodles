namespace Moodles.Moodles.Services.Data;

internal enum MoodleRemoveReason
{
    Timeout,
    ManualNoFlag,
    ManualFlag,
    Death,
    Esuna,
    Reflush,
    IPCNoFlag,
    IPCFlag,
    Rat,        // This just means this moodle has a Guid that isnt in the database
}
