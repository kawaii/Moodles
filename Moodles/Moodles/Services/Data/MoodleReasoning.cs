namespace Moodles.Moodles.Services.Data;

internal enum MoodleReasoning
{
    Timeout,
    ManualNoFlag,   // No flag means it does NOT trigger the apply on death moodle
    ManualFlag,
    Death,
    Esuna,
    Reflush,
    IPCNoFlag,      // No flag means it does NOT trigger the apply on death moodle
    IPCFlag,
    Rat,            // This just means this moodle has a Guid that isn't in the database
}
