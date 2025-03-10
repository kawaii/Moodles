using Dalamud.Plugin.Services;
using System;

namespace Moodles.Moodles.StatusManaging.Interfaces;

internal interface IMoodlesDatabase
{
    IMoodleStatusManager[] StatusManagers { get; }
    IMoodle[] Moodles { get; }

    IMoodleStatusManager? GetStatusManagerNoCreate(ulong contentID, int skeletonID);
    IMoodleStatusManager GetPlayerStatusManager(ulong contentID);
    IMoodleStatusManager GetPetStatusManager(ulong contentID, int skeletonID);

    IMoodle? GetMoodle(WorldMoodle wMoodle);
    IMoodle? GetMoodleNoCreate(Guid identifier);
    IMoodle CreateMoodle(bool isEphemiral = false);
    void RegisterMoodle(IMoodle moodle, bool fromIPC = false);

    void RemoveMoodle(IMoodle moodle);
    void RemoveStatusManager(IMoodleStatusManager entry);

    void Update(IFramework framework);

    void PrepareForSave();
    void CleanupSave();
}
