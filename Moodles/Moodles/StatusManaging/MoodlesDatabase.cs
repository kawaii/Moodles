using Dalamud.Plugin.Services;
using Moodles.Moodles.Mediation;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using System.Collections.Generic;

namespace Moodles.Moodles.StatusManaging;

internal class MoodlesDatabase : IMoodlesDatabase
{
    public IMoodleStatusManager[] StatusManagers => _statusManagers.ToArray();
    public IMoodle[] Moodles => _moodles.ToArray();

    readonly List<IMoodleStatusManager> _statusManagers = new List<IMoodleStatusManager>();
    readonly List<IMoodle> _moodles = new List<IMoodle>();

    readonly IMoodlesServices Services;

    public MoodlesDatabase(IMoodlesServices services)
    {
        Services = services;

        FloodDatabase();
    }

    void FloodDatabase()
    {
        foreach (Moodle moodle in Services.Configuration.SavedMoodles)
        {
            RegisterMoodle(moodle);
        }

        Services.Configuration.SavedMoodles.Clear();
    }

    public void PrepareForSave()
    {
        CleanupSave();

        int moodlesCount = _moodles.Count;

        for (int i = 0; i < moodlesCount; i++)
        {
            IMoodle moodle = _moodles[i];
            if (moodle is not Moodle mMoodle) continue;
            if (moodle.IsEphemeral) continue;

            Services.Configuration.SavedMoodles.Add(mMoodle);
        }
    }

    public void CleanupSave()
    {
        Services.Configuration.SavedMoodles.Clear();
    }

    public void Update(IFramework framework)
    {
        foreach (IMoodleStatusManager statusManager in _statusManagers)
        {
            statusManager.Update(framework);
        }
    }

    public IMoodle? GetMoodleNoCreate(Guid identifier)
    {
        int moodlesCount = _moodles.Count;

        for (int i = 0; i < moodlesCount; i++)
        {
            IMoodle moodle = _moodles[i];
            if (moodle.Identifier != identifier) continue;

            return moodle;
        }

        return null;
    }

    public void RegisterMoodle(IMoodle moodle, bool fromIPC = false)
    {
        RemoveMoodle(moodle);

        moodle.SetEphemeral(fromIPC);   // Don't notify

        _moodles.Add(moodle);

        Services.Mediator.Send(new DatabaseAddedMoodleMessage(this, moodle));
        Services.Mediator.Send(new DatabaseDirtyMessage(this));
    }

    public IMoodle CreateMoodle(bool isEphemiral = false)
    {
        IMoodle newMoodle = new Moodle();
        _moodles.Add(newMoodle);
        newMoodle.SetEphemeral(isEphemiral);

        Services.Mediator.Send(new DatabaseAddedMoodleMessage(this, newMoodle));
        Services.Mediator.Send(new DatabaseDirtyMessage(this));

        return newMoodle;
    }

    public void RemoveMoodle(IMoodle moodle)
    {
        for (int i = _moodles.Count - 1; i >= 0; i--)
        {
            if (_moodles[i].Identifier != moodle.Identifier) continue;

            PluginLog.LogVerbose($"Removed moodle for: {moodle.Identifier} {moodle.Title}");

            Services.Mediator.Send(new DatabaseRemovedMoodleMessage(this, _moodles[i]));
            Services.Mediator.Send(new DatabaseDirtyMessage(this));

            _moodles.RemoveAt(i);
        }
    }

    public IMoodleStatusManager? GetStatusManagerNoCreate(ulong contentID, int skeletonID)
    {
        int entriesCount = _statusManagers.Count;

        for (int i = 0; i < entriesCount; i++)
        {
            IMoodleStatusManager entry = _statusManagers[i];
            if (entry.ContentID != contentID) continue;
            if (entry.SkeletonID != skeletonID) continue;

            return _statusManagers[i];
        }

        return null;
    }

    public IMoodleStatusManager GetPlayerStatusManager(ulong contentID)
    {
        return GetPetStatusManager(contentID, PluginConstants.PlayerSkeleton);
    }

    public IMoodleStatusManager GetPetStatusManager(ulong contentID, int skeletonID)
    {
        IMoodleStatusManager? statusManager = GetStatusManagerNoCreate(contentID, skeletonID);
        if (statusManager != null) return statusManager;

        PluginLog.LogVerbose($"Created status manager for: {contentID} {skeletonID}");

        IMoodleStatusManager newStatusManager = new MoodlesStatusManager(Services, contentID, skeletonID, "[UNKNOWN]", 0, false);
        _statusManagers.Add(newStatusManager);

        Services.Mediator.Send(new DatabaseAddedStatusManagerMessage(this, newStatusManager));
        Services.Mediator.Send(new DatabaseDirtyMessage(this));

        return newStatusManager;
    }

    public void RemoveStatusManager(IMoodleStatusManager entry)
    {
        for (int i = _statusManagers.Count - 1; i >= 0; i--)
        {
            if (_statusManagers[i].ContentID != entry.ContentID) continue;
            if (_statusManagers[i].SkeletonID != entry.SkeletonID) continue;

            PluginLog.LogVerbose($"Removed status manager for: {entry.Name}");

            Services.Mediator.Send(new DatabaseRemovedStatusManagerMessage(this, _statusManagers[i]));
            Services.Mediator.Send(new DatabaseDirtyMessage(this));

            _statusManagers.RemoveAt(i);
        }
    }
}
