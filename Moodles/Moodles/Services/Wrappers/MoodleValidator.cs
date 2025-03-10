using ECommons.ExcelServices.TerritoryEnumeration;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Moodles.Moodles.Services.Wrappers;

internal class MoodleValidator : IMoodleValidator
{
    readonly ISheets Sheets;

    public MoodleValidator(ISheets sheets)
    {
        Sheets = sheets;
    }

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
        
        _ = Utils.ParseBBSeString(moodle.Title, out string? titleParseError);
        if (titleParseError != null)
        {
            error = $"Syntax error in title: {titleParseError}";
            return false;
        }


        _ = Utils.ParseBBSeString(moodle.Description, out string? descriptionParseError);
        if (descriptionParseError != null)
        {
            error = $"Syntax error in description: {descriptionParseError}";
            return false;
        }

        error = null;
        return true;
    }

    public int GetMoodleDuration(IMoodle moodle)
    {
        return moodle.Days + moodle.Hours + moodle.Seconds + moodle.Minutes;
    }

    public int GetMoodleDuration(IMoodle moodle, out int days, out int hours, out int minutes, out int seconds, out bool countDownWhenOffline)
    {
        days = moodle.Days;
        hours = moodle.Hours;
        minutes = moodle.Minutes;
        seconds = moodle.Seconds;
        countDownWhenOffline = moodle.CountsDownWhenOffline;

        return GetMoodleDuration(moodle);
    }

    public uint GetAdjustedIconId(uint iconId, uint currentStackSize)
    {
        return iconId + currentStackSize - 1;
    }

    public uint GetMaxStackSize(uint iconId)
    {
        uint? stackSize = Sheets.GetStackCount(iconId);
        if (stackSize == null) return 1;

        return stackSize.Value;
    }

    public bool CanApplyStacks(uint iconId, uint currentStackSize, uint stacksToApply)
    {
        uint maxStackSize = GetMaxStackSize(iconId);

        uint newStackSize = currentStackSize + stacksToApply;

        return newStackSize <= maxStackSize;
    }

    public long GetMoodleTickTime(WorldMoodle wMoodle, IMoodle moodle)
    {
        if (wMoodle.Identifier != moodle.Identifier) return 0;

        if (moodle.CountsDownWhenOffline)
        {
            return DateTime.Now.Ticks - wMoodle.AppliedOn;
        }

        return wMoodle.TickedTime;
    }

    public long MoodleLifetime(IMoodle moodle)
    {
        if (moodle.Permanent) return -1;

        // 10000 for ticks
        return 10000 * (moodle.Seconds * 1000 + moodle.Minutes * 1000 * 60 + moodle.Hours * 1000 * 60 * 60 + moodle.Days * 1000 * 60 * 60 * 24);
    }

    public bool MoodleOverTime(WorldMoodle wMoodle, IMoodle moodle)
    {
        if (wMoodle.Identifier != moodle.Identifier) return false;

        if (moodle.Permanent) return false;

        long lifetime = MoodleLifetime(moodle);
        long tickedTime = GetMoodleTickTime(wMoodle, moodle);

        if (tickedTime > lifetime)
        {
            return true;
        }

        return false;
    }
}
