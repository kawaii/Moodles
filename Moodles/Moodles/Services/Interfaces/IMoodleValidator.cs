using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Moodles.Moodles.Services.Interfaces;

internal interface IMoodleValidator
{
    bool IsValid(IMoodle moodle, [NotNullWhen(false)] out string? error);

    long GetMoodleDuration(IMoodle moodle);
    long GetMoodleDuration(IMoodle moodle, out int days, out int hours, out int minutes, out int seconds, out bool countDownWhenOffline);

    uint GetMaxStackSize(uint iconId);
    bool CanApplyStacks(uint iconId, uint currentStackSize, uint stacksToApply);

    uint GetAdjustedIconId(uint iconId, uint currentStackSize);

    long GetMoodleTickTime(WorldMoodle wMoodle, IMoodle moodle);
    long MoodleLifetime(IMoodle moodle);
    bool MoodleOverTime(WorldMoodle wMoodle, IMoodle moodle, out long overTime);
}
