using Dalamud.Plugin.Services;

namespace Moodles.Moodles.StatusManaging.Interfaces;

internal interface IMoodlesDatabase
{
    IMoodleStatusManager[] StatusManagers { get; }

    IMoodleStatusManager? GetStatusManagerNoCreate(ulong contentID, int skeletonID);
    IMoodleStatusManager GetPlayerStatusManager(ulong contentID);
    IMoodleStatusManager GetPetStatusManager(ulong contentID, int skeletonID);

    void RemoveStatusManager(IMoodleStatusManager entry);

    void UpdateStatusManagers(IFramework framework);
}
