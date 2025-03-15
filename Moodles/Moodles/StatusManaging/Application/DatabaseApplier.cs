using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moodles.Moodles.StatusManaging.Application;

internal class DatabaseApplier
{
    readonly IMoodlesDatabase Database;
    readonly IMoodlesServices Services;
    readonly IUserList UserList;

    int moodleTicker = 0;

    public DatabaseApplier(IMoodlesDatabase database, IMoodlesServices services, IUserList userList)
    {
        Database = database;
        Services = services;
        UserList = userList;
    }

    public void FloodDatabase()
    {
        PluginLog.LogInfo("[Start Flood Database]");

        foreach (Moodle moodle in Services.Configuration.SavedMoodles)
        {
            PluginLog.LogInfo($"[Register Moodle] [{moodle.Identifier}] [{moodle.Title}]");
            Database.RegisterMoodle(moodle, moodle.IsEphemeral);
        }

        foreach (MoodlesStatusManager statusManager in Services.Configuration.SavedStatusManagers)
        {
            PluginLog.LogInfo($"[Register StatusManager] [{statusManager.ContentID}] [{statusManager.SkeletonID}]");
            ApplyStatusManager(statusManager, false);
        }

        Database.CleanupSave();
    }

    public void Apply(IMoodle[] moodles, bool fromIPC)
    {
        foreach (IMoodle newMoodle in moodles)
        {
            bool notContained = true;

            foreach (IMoodle databaseMoodle in Database.Moodles)
            {
                if (newMoodle.Identifier != databaseMoodle.Identifier) continue;

                notContained = false;
                databaseMoodle.Apply(newMoodle);
                break;
            }

            if (!notContained) continue;

            Database.RegisterMoodle(newMoodle, fromIPC);
        }
    }

    void ApplyStatusManager(IMoodleStatusManager statusManager, bool fromIPC)
    {
        if (fromIPC)
        {
            if (UserList.GetUserFromContentID(statusManager.ContentID) == null)
            {
                PluginLog.LogInfo($"IPC Status Manager Failed to Add for user: [{statusManager.ContentID}] [{statusManager.SkeletonID}] because they weren't found in the world.");
                return;
            }
        }

        List<WorldMoodle> heldMoodles = statusManager.WorldMoodles.ToList();

        PluginLog.LogInfo($"Status Manager: [{statusManager.ContentID}] [{statusManager.SkeletonID}] world moodles stored and cleared.");

        IMoodleStatusManager currentManager = Database.GetPetStatusManager(statusManager.ContentID, statusManager.SkeletonID);

        currentManager.WorldMoodles.Clear();

        PluginLog.LogInfo($"Status Manager: [{currentManager.ContentID}] [{currentManager.SkeletonID}] finalized registry.");

        int worldMoodleCount = heldMoodles.Count;

        PluginLog.LogInfo($"World Moodle Count: {worldMoodleCount}.");

        for (int i = 0; i < worldMoodleCount; i++)
        {
            WorldMoodle moodle = heldMoodles[i];

            PluginLog.LogInfo($"Handling Moodle: [{moodle.Identifier}] [{moodle.AppliedOn}] [{moodle.TickedTime}]");

            IMoodle? mirrorMoodle = Database.GetMoodle(moodle);
            if (mirrorMoodle == null) continue;

            PluginLog.LogInfo($"Which is mirror moodle: [{mirrorMoodle.Identifier}] [{mirrorMoodle.Title}]");

            if (!mirrorMoodle.CountsDownWhenOffline)
            {
                PluginLog.LogInfo($"Moodle doestn count down offline so it has been applied.");
                currentManager.ApplyMoodle(mirrorMoodle, moodle, MoodleReasoning.Reflush, Services.MoodleValidator, UserList, Services.Mediator);
            }
            else
            {
                PluginLog.LogInfo($"Moodle DOES count down when offline, we are entering advanced setup mode.");
                AdvancedApplyMoodle(currentManager, moodle, mirrorMoodle);
            }
        }
    }

    void AdvancedApplyMoodle(IMoodleStatusManager statusManager, WorldMoodle moodle, IMoodle mirrorMoodle)
    {
        if (!Services.MoodleValidator.MoodleOverTime(moodle, mirrorMoodle, out long overTime))
        {
            PluginLog.LogInfo($"Current moodle entered advanced setup but isnt over time yet. It has been applied");
            statusManager.ApplyMoodle(mirrorMoodle, moodle, MoodleReasoning.Reflush, Services.MoodleValidator, UserList, Services.Mediator);
            return;
        }

        moodleTicker = 0;

        PluginLog.LogInfo($"Moodle applier started. moodleTicker is {moodleTicker}");

        ApplyStatus status = DoApply(statusManager, moodle, mirrorMoodle);

        PluginLog.LogInfo($"Moodle applied with status: {status}");
    }

    ApplyStatus DoApply(IMoodleStatusManager statusManager, WorldMoodle moodle, IMoodle mirrorMoodle)
    {
        moodleTicker++;

        PluginLog.LogInfo($"MoodleTicker is {moodleTicker}. Moodle is: [{mirrorMoodle.Identifier}] [{mirrorMoodle.Title}]");

        if (!mirrorMoodle.CountsDownWhenOffline)
        {
            PluginLog.LogInfo($"Moodle doestn count down offline so it has been applied.");
            statusManager.ApplyMoodle(mirrorMoodle, moodle, MoodleReasoning.ManualFlag, Services.MoodleValidator, UserList, Services.Mediator);
            return ApplyStatus.Applied;
        }

        if (mirrorMoodle.StatusOnDispell == Guid.Empty)     // End of the chain
        {
            PluginLog.LogInfo($"StatusOnDispell is {Guid.Empty}. The moodle has been removed");

            statusManager.ApplyMoodle(mirrorMoodle, MoodleReasoning.Reflush, Services.MoodleValidator, UserList);       // No mediator. This is a formality because you can only remove moodles that you ahve applied
            statusManager.RemoveMoodle(mirrorMoodle, MoodleReasoning.Timeout, Services.Mediator);
            return ApplyStatus.Null;
        }

        if (moodleTicker > PluginConstants.MaxTimerDepthSearch)     // Recursion Limiter
        {
            PluginLog.LogInfo($"moodleTicker is over the limit [{PluginConstants.MaxTimerDepthSearch}]. We stop the search and apply the last applicable moodle.");

            statusManager.ApplyMoodle(mirrorMoodle, MoodleReasoning.ManualFlag, Services.MoodleValidator, UserList, Services.Mediator);       // No mediator. This is a formality because you can only remove moodles that you have applied
            return ApplyStatus.Clamped;
        }

        if (!Services.MoodleValidator.MoodleOverTime(moodle, mirrorMoodle, out long overTime))  // Means this moodle should be active c:
        {
            statusManager.ApplyMoodle(mirrorMoodle, MoodleReasoning.Reflush, Services.MoodleValidator, UserList, Services.Mediator);

            WorldMoodle? newWMoodle = statusManager.GetMoodle(mirrorMoodle);
            if (newWMoodle != null)
            {
                newWMoodle.AppliedOn += overTime;   // This offsets the time it shouldve been running for c:
            }

            PluginLog.LogInfo($"The moodle at moodleticker: [{moodleTicker}] is valid now. OverTime: [{overTime}]");

            return ApplyStatus.Applied;
        }

        PluginLog.LogInfo($"OverTime: [{overTime}]. MoodleTime: [{Services.MoodleValidator.GetMoodleTickTime(moodle, mirrorMoodle)}]");

        WorldMoodle tempWorldMoodle = new WorldMoodle()
        {
            Identifier = mirrorMoodle.StatusOnDispell,
            AppliedOn = DateTime.Now.Ticks - overTime,
            StackCount = moodle.StackCount,
            TickedTime = 0,
            AppliedBy = moodle.AppliedBy,
        };

        IMoodle? newMirrorMoodle = Database.GetMoodle(tempWorldMoodle);
        if (newMirrorMoodle == null)
        {
            PluginLog.LogInfo($"Moodle wasnt found in the database");
            return ApplyStatus.Failed;
        }

        PluginLog.LogInfo($"Call the child c:");

        ApplyStatus status = DoApply(statusManager, tempWorldMoodle, newMirrorMoodle);

        if (status == ApplyStatus.Failed)
        {
            PluginLog.LogInfo($"Moodle failed!");

            WorldMoodle? newWMoodle = statusManager.GetMoodle(mirrorMoodle);
            if (newWMoodle != null)
            {
                newWMoodle.AppliedOn += overTime;   // This offsets the time it shouldve been running for c:
            }

            PluginLog.LogInfo($"Applied last moodle instead!");

            return ApplyStatus.Applied;
        }

        return status;
    }

    public void Apply(IMoodleStatusManager statusManager, WorldMoodle[] worldMoodles, bool fromIPC)
    {

    }

    enum ApplyStatus
    { 
        Applied,
        Null,
        Clamped,
        Failed
    }
}
