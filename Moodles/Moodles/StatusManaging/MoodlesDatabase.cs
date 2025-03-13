using Dalamud.Plugin.Services;
using Moodles.Moodles.Mediation;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using System.Collections.Generic;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.ILayoutInstance;

namespace Moodles.Moodles.StatusManaging;

internal class MoodlesDatabase : IMoodlesDatabase
{
    public IMoodleStatusManager[] StatusManagers => _statusManagers.ToArray();
    public IMoodle[] Moodles => _moodles.ToArray();

    readonly List<IMoodleStatusManager> _statusManagers = new List<IMoodleStatusManager>();
    readonly List<IMoodle> _moodles = new List<IMoodle>();

    readonly IMoodlesServices Services;
    readonly IUserList UserList;

    public MoodlesDatabase(IMoodlesServices services, IUserList userList)
    {
        Services = services;
        UserList = userList;

        FloodDatabase();
    }

    void FloodDatabase()
    {
        foreach (Moodle moodle in Services.Configuration.SavedMoodles)
        {
            RegisterMoodle(moodle);
        }

        foreach (MoodlesStatusManager statusManager in Services.Configuration.SavedStatusManagers)
        {
            RegisterStatusManager(statusManager);
        }

        CleanupSave();
    }

    public void PrepareForSave()
    {
        CleanupSave();

        int moodlesCount = _moodles.Count;

        for (int i = 0; i < moodlesCount; i++)
        {
            try
            {
                IMoodle moodle = _moodles[i];
                if (!moodle.Savable(this)) continue;
                if (moodle is not Moodle mMoodle) continue;

                Services.Configuration.SavedMoodles.Add(mMoodle);
            }
            catch(Exception e)
            {
                PluginLog.LogException(e);
            }
        }

        int statusManagerCount = _statusManagers.Count;

        for (int i = 0; i < statusManagerCount; i++)
        {
            try
            {
                IMoodleStatusManager statusManager = _statusManagers[i];
                if (!statusManager.Savable()) continue;
                if (statusManager is not MoodlesStatusManager sManager) continue;

                Services.Configuration.SavedStatusManagers.Add(sManager);
            }
            catch (Exception e)
            {
                PluginLog.LogException(e);
            }
        }
    }

    public void CleanupSave()
    {
        Services.Configuration.SavedMoodles.Clear();
        Services.Configuration.SavedStatusManagers.Clear();
    }

    public void Update(IFramework framework)
    {
        int statusManagerCount = _statusManagers.Count;

        for (int i = 0; i < statusManagerCount; i++)
        {
            IMoodleStatusManager statusManager = _statusManagers[i];

            IMoodleUser? user = UserList.GetUserFromContentID(statusManager.ContentID);

            if (user != null)
            {
                _statusManagers[i].Update(framework);
            }

            _statusManagers[i].ValidateMoodles(framework, Services.MoodleValidator, this, user, Services.Mediator);
        }
    }

    public IMoodle? GetMoodle(WorldMoodle wMoodle)
    {
        int moodlesCount = _moodles.Count;

        for (int i = 0; i < moodlesCount; i++)
        {
            IMoodle moodle = _moodles[i];
            if (moodle.Identifier != wMoodle.Identifier) continue;

            return moodle;
        }

        return null;
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

    public void RegisterStatusManager(IMoodleStatusManager statusManager, bool fromIpc = false)
    {
        RemoveStatusManager(statusManager);

        statusManager.SetEphemeralStatus(fromIpc);  // Don't Notify

        _statusManagers.Add(statusManager);

        Services.Mediator.Send(new DatabaseAddedStatusManagerMessage(this, statusManager));
        Services.Mediator.Send(new DatabaseDirtyMessage(this));
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

        IMoodleStatusManager newStatusManager = new MoodlesStatusManager(contentID, skeletonID);
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

            PluginLog.LogVerbose($"Removed status manager for: {entry.ContentID}");

            Services.Mediator.Send(new DatabaseRemovedStatusManagerMessage(this, _statusManagers[i]));
            Services.Mediator.Send(new DatabaseDirtyMessage(this));

            _statusManagers.RemoveAt(i);
        }
    }
}
