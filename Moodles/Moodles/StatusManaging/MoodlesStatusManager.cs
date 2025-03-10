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
            WorldMoodles[i].Update(framework);
        }
    }

    public unsafe void ValidateMoodles(IFramework framework, IMoodleValidator validator, IMoodlesDatabase database, IUserList userList, IMoodlesMediator? mediator = null)
    {
        int worldMoodleCount = WorldMoodles.Count;

        for (int i = worldMoodleCount - 1; i >= 0; i--)
        {
            WorldMoodle wMoodle = WorldMoodles[i];
            IMoodle? moodle = database.GetMoodle(wMoodle);

            if (moodle == null)
            {
                RemoveMoodle(wMoodle, MoodleRemoveReason.Rat, mediator);
                continue;
            }

            if (validator.MoodleOverTime(wMoodle, moodle))
            {
                RemoveMoodle(wMoodle, MoodleRemoveReason.Timeout, mediator);
                continue;
            }

            if (moodle.DispellsOnDeath)
            {
                IMoodleUser? user = userList.GetUserFromContentID(ContentID);
                if (user == null)
                {
                    wMoodle.RemoveTickedTime(framework);
                }
                else if (user.Self->Health == 0)
                {
                    RemoveMoodle(wMoodle, MoodleRemoveReason.Death, mediator);
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

    public void ApplyMoodle(IMoodle moodle, IMoodleValidator moodleValidator, IMoodlesMediator? mediator = null)
    {
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
                AppliedBy = 0,
                AppliedOn = DateTime.Now.Ticks
            };

            PluginLog.Log($"Applied moodle: [{moodle.Identifier}] to user [{ContentID}].");

            WorldMoodles.Add(newMoodle);
            mediator?.Send(new MoodleAppliedMessage(moodle, newMoodle, this));

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

    public void RemoveMoodle(IMoodle moodle, MoodleRemoveReason removeReason, IMoodlesMediator? mediator = null)
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

    public void RemoveMoodle(WorldMoodle wMoodle, MoodleRemoveReason removeReason, IMoodlesMediator? mediator = null)
    {
        if (WorldMoodles.Remove(wMoodle))
        {
            mediator?.Send(new MoodleRemovedMessage(wMoodle, removeReason, this));
        }
    }
}
