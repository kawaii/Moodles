using Dalamud.Plugin.Services;
using Moodles.Moodles.Mediation.Interfaces;
using System.Collections.Generic;

namespace Moodles.Moodles.StatusManaging.Interfaces;

internal interface IMoodleStatusManager
{
    bool IsActive { get;  }
    bool IsEphemeral { get; }

    ulong ContentID { get; } // Player or owner content ID
    int SkeletonID { get; } // Player skeleton is 0

    List<WorldMoodle> WorldMoodles { get; }

    void Update(IFramework framework);

    void SetActive(bool active, IMoodlesMediator? mediator = null);
    void SetEphemeralStatus(bool ephemeralStatus, IMoodlesMediator? mediator = null);

    void Clear(IMoodlesMediator? mediator = null);

    bool Savable();
}
