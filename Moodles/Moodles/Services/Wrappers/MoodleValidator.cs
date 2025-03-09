using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Moodles.Moodles.Services.Wrappers;

internal class MoodleValidator : IMoodleValidator
{
    public bool IsValid(IMoodle moodle, [NotNullWhen(false)] out string? error)
    {
        if (moodle.IconID == 0)
        {
            error = ("Icon is not set");
            return false;
        }
        if (moodle.IconID < 200000)
        {
            error = ("Icon is a Pre 7.1 Moodle!");
            return false;
        }
        if (moodle.Title.Length == 0)
        {
            error = ("Title is not set");
            return false;
        }

        int totalTime = GetMoodleDuration(moodle);

        if (totalTime < 1 && !moodle.Permanent)
        {
            error = ("Duration is not set");
            return false;
        }
        
        /*
        Utils.ParseBBSeString(Title, out var parseError);
        if (parseError != null)
        {
            error = $"Syntax error in title: {parseError}";
            return false;
        }
        
        
        Utils.ParseBBSeString(Description, out var parseError);
        if (parseError != null)
        {
            error = $"Syntax error in description: {parseError}";
            return false;
        }*/

        error = null;
        return true;
    }

    public int GetMoodleDuration(IMoodle moodle)
    {
        return moodle.Days + moodle.Hours + moodle.Seconds + moodle.Minutes;
    }

    public int GetMoodleDuration(IMoodle moodle, out int days, out int hours, out int minutes, out int seconds)
    {
        days = moodle.Days;
        hours = moodle.Hours;
        minutes = moodle.Minutes;
        seconds = moodle.Seconds;

        return GetMoodleDuration(moodle);
    }

    public uint GetAdjustedIconId(IMoodle moodle)
    {
        return (uint)(moodle.IconID + moodle.StartingStacks - 1);
    }
}
