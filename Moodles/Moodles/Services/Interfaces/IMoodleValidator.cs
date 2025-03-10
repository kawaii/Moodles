using Moodles.Moodles.StatusManaging.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Moodles.Moodles.Services.Interfaces;

internal interface IMoodleValidator
{
    bool IsValid(IMoodle moodle, [NotNullWhen(false)] out string? error);

    int GetMoodleDuration(IMoodle moodle);
    int GetMoodleDuration(IMoodle moodle, out int days, out int hours, out int minutes, out int seconds, out bool countDownWhenOffline);

    uint GetAdjustedIconId(IMoodle moodle);
}
