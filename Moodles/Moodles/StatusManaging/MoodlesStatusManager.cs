using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Plugin.Services;
using MemoryPack;
using Moodles.Moodles.Mediation;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using Newtonsoft.Json;

namespace Moodles.Moodles.StatusManaging;

[Serializable]
[MemoryPackable]
internal partial class MoodlesStatusManager : IMoodleStatusManager
{
    [MemoryPackIgnore][JsonIgnore] public bool IsEphemeral { get; private set; } = false;

    public ulong ContentID { get; set; }     // The owners contentID if this is a pets status manager

    public int SkeletonID { get; set; }      // The pets skeleton, is 0 if it is a player

    public List<WorldMoodle> WorldMoodles { get; set; } = new List<WorldMoodle>();

    [MemoryPackConstructor]
    [JsonConstructor]
    public MoodlesStatusManager(ulong contentID, int skeletonID)
    {
        ContentID = contentID;
        SkeletonID = skeletonID;
    }

    public void Update(IFramework framework)
    {
        int worldMoodleCount = WorldMoodles.Count;

        for (int i = worldMoodleCount - 1; i >= 0; i--)
        {
            // Tick every moodle, even permanent ones because the timer can change later
            WorldMoodles[i].Update(framework);
        }
    }

    public unsafe void ValidateMoodles(IFramework framework, IMoodleValidator validator, IMoodlesDatabase database, IMoodleUser? user, IMoodlesMediator? mediator = null)
    {
        int worldMoodleCount = WorldMoodles.Count;

        for (int i = worldMoodleCount - 1; i >= 0; i--)
        {
            WorldMoodle wMoodle = WorldMoodles[i];
            IMoodle? moodle = database.GetMoodle(wMoodle);

            if (moodle == null)
            {
                RemoveMoodle(wMoodle, MoodleReasoning.Rat, mediator);
                continue;
            }

            if (validator.MoodleOverTime(wMoodle, moodle, out _))
            {
                RemoveMoodle(wMoodle, MoodleReasoning.Timeout, mediator);
                continue;
            }

            if (moodle.DispellsOnDeath && user != null)
            {
                if (user.Self->Health == 0)
                {
                    RemoveMoodle(wMoodle, MoodleReasoning.Death, mediator);
                    continue;
                }
            }
        }
    }

    public void SetEphemeralStatus(bool ephemeralStatus, IMoodlesMediator? mediator = null)
    {
        IsEphemeral = ephemeralStatus;

        mediator?.Send(new StatusManagerDirtyMessage(this));
    }

    public void Clear(IMoodlesMediator? mediator = null)
    {
        WorldMoodles.Clear();

        mediator?.Send(new StatusManagerDirtyMessage(this));
    }

    public bool Savable()
    {
        if (IsEphemeral) return false;
        if (SkeletonID > 0) return false;   // Dont save status managers of minions
        if (WorldMoodles.Count == 0) return false;

        return true;
    }

    public bool HasMoodle(IMoodle moodle, [NotNullWhen(true)] out WorldMoodle? wMoodle)
    {
        wMoodle = null;

        int moodleCount = WorldMoodles.Count;

        for (int i = 0; i < moodleCount; i++)
        {
            wMoodle = WorldMoodles[i];
            if (wMoodle.Identifier != moodle.Identifier) continue;

            return true;
        }

        return false;
    }

    public bool HasMaxedOutMoodle(IMoodle moodle, IMoodleValidator moodleValidator, [NotNullWhen(true)] out WorldMoodle? wMoodle)
    {
        wMoodle = null;

        int moodleCount = WorldMoodles.Count;

        for (int i = 0; i < moodleCount; i++)
        {
            wMoodle = WorldMoodles[i];
            if (wMoodle.Identifier != moodle.Identifier) continue;

            if (!moodle.StackOnReapply)
            {
                return true;
            }

            if (moodleValidator.CanApplyStacks((uint)moodle.IconID, wMoodle.StackCount, (uint)moodle.StackIncrementOnReapply))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    public void ApplyMoodle(IMoodle moodle, MoodleReasoning applyReason, IMoodleValidator moodleValidator, IUserList userList, IMoodlesMediator? mediator = null)
    {
        if (WorldMoodles.Count >= PluginConstants.MoodleMax)
        {
            PluginLog.LogFatal($"You've reached the max amount of moodles on this status manager.");
            return;
        }

        if (!moodleValidator.IsValid(moodle, out string? error))
        {
            PluginLog.Log($"Please do not apply a moodle that is Invalid: {error}");
            return;
        }    

        if (!HasMoodle(moodle, out WorldMoodle? wMoodle))
        {
            WorldMoodle newMoodle = new WorldMoodle()
            {
                Identifier = moodle.Identifier,
                StackCount = (uint)moodle.StartingStacks,
                AppliedBy = userList.LocalPlayer?.ContentID ?? 0,
                AppliedOn = DateTime.Now.Ticks
            };

            PluginLog.Log($"Applied moodle: [{moodle.Identifier}] to user [{ContentID}].");

            WorldMoodles.Add(newMoodle);
            mediator?.Send(new MoodleAppliedMessage(moodle, applyReason, newMoodle, this));

            return;
        }

        if (!HasMaxedOutMoodle(moodle, moodleValidator, out _))
        {
            PluginLog.Log($"Applied stacks to moodle: [{moodle.Identifier}] for user [{ContentID}].");

            wMoodle.AddStacksUnchecked((uint)moodle.StackIncrementOnReapply, moodle.TimeResetsOnStack, mediator);

            return;
        }

        PluginLog.Log($"Tried to apply moodle: [{moodle.Identifier}] which the user [{ContentID}] already had.");        
    }

    public void ApplyMoodle(IMoodle moodle, WorldMoodle wMoodle, MoodleReasoning applyReason, IMoodleValidator moodleValidator, IUserList userList, IMoodlesMediator? mediator = null)
    {
        int moodleCount = WorldMoodles.Count;

        for (int i = 0; i < moodleCount; i++)
        {
            WorldMoodle worldMoodle = WorldMoodles[i];
            if (worldMoodle.Identifier != moodle.Identifier) continue;

            worldMoodle.AppliedOn = wMoodle.AppliedOn;
            worldMoodle.AppliedBy = wMoodle.AppliedBy;
            worldMoodle.Identifier = wMoodle.Identifier;
            worldMoodle.StackCount = wMoodle.StackCount;
            worldMoodle.TickedTime = wMoodle.TickedTime;

            mediator?.Send(new MoodleAppliedMessage(moodle, applyReason, wMoodle, this));

            return;
        }

        WorldMoodles.Add(wMoodle);
        mediator?.Send(new MoodleAppliedMessage(moodle, applyReason, wMoodle, this));
    }

    public void RemoveMoodle(IMoodle moodle, MoodleReasoning removeReason, IMoodlesMediator? mediator = null)
    {
        int moodleCount = WorldMoodles.Count;

        for (int i = moodleCount - 1; i >= 0; i--)
        {
            WorldMoodle wMoodle = WorldMoodles[i];
            if (wMoodle.Identifier != moodle.Identifier) continue;

            RemoveMoodle(wMoodle, removeReason, mediator);
            return;
        }
    }

    public void RemoveMoodle(WorldMoodle wMoodle, MoodleReasoning removeReason, IMoodlesMediator? mediator = null)
    {
        if (WorldMoodles.Remove(wMoodle))
        {
            mediator?.Send(new MoodleRemovedMessage(wMoodle, removeReason, this));
        }
    }

    public WorldMoodle? GetMoodle(IMoodle moodle)
    {
        int moodleCount = WorldMoodles.Count;

        for (int i = moodleCount - 1; i >= 0; i--)
        {
            WorldMoodle wMoodle = WorldMoodles[i];
            if (wMoodle.Identifier != moodle.Identifier) continue;

            return wMoodle;
        }

        return null;
    }
}
