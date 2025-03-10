using Dalamud.Plugin.Services;
using Moodles.Moodles.SaveHandling;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Updating.Interfaces;
using System.Collections.Generic;

namespace Moodles.Moodles.Updating;

internal class UpdateHandler : IUpdateHandler
{
    readonly DalamudServices DalamudServices;
    readonly IMoodlesServices MoodlesServices;
    readonly SaveHandler SaveHandler;

    readonly List<IUpdatable> _updatables = new List<IUpdatable>();

    public UpdateHandler(DalamudServices dalamudServices, IMoodlesServices moodlesServices, SaveHandler saveHandler)
    {
        DalamudServices = dalamudServices;
        MoodlesServices = moodlesServices;
        SaveHandler = saveHandler;

        DalamudServices.Framework.Update += OnUpdate;

        _Register();
    }

    void _Register()
    {
        Register(SaveHandler);
    }

    void Register(IUpdatable updatable)
    {
        _updatables.Add(updatable);
    }

    void OnUpdate(IFramework framework)
    {
        int updatableCount = _updatables.Count;
        for (int i = 0; i < updatableCount; i++)
        {
            IUpdatable updatable = _updatables[i];
            if (!updatable.Enabled) continue;
            updatable.Update(framework);
        }
    }

    public void Dispose()
    {
        DalamudServices.Framework.Update -= OnUpdate;
    }
}
