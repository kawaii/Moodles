using Dalamud.Plugin.Services;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using System.Collections.Generic;

namespace Moodles.Moodles.StatusManaging;

internal class MoodlesDatabase : IMoodlesDatabase
{
    public IMoodleStatusManager[] StatusManagers => _statusManagers.ToArray();

    readonly List<IMoodleStatusManager> _statusManagers = new List<IMoodleStatusManager>();

    readonly IMoodlesServices Services;

    public MoodlesDatabase(IMoodlesServices services)
    {
        Services = services;
    }

    public void UpdateStatusManagers(IFramework framework)
    {
        foreach (IMoodleStatusManager statusManager in _statusManagers)
        {
            statusManager.Update(framework);
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
        return newStatusManager;
    }

    public void RemoveStatusManager(IMoodleStatusManager entry)
    {
        for (int i = _statusManagers.Count - 1; i >= 0; i--)
        {
            if (_statusManagers[i].ContentID != entry.ContentID) continue;
            if (_statusManagers[i].SkeletonID != entry.SkeletonID) continue;

            PluginLog.LogVerbose($"Removed status manager for: {entry.Name}");

            _statusManagers.RemoveAt(i);
        }
    }
}
