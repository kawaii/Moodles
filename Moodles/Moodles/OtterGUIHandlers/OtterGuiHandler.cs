using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using OtterGui.Log;
using System;

namespace Moodles.Moodles.OtterGUIHandlers;

internal sealed class OtterGuiHandler : IDisposable
{
    public readonly MoodleFileSystem MoodleFileSystem;
    public readonly Logger Logger;

    public OtterGuiHandler(DalamudServices dalamudServices, IMoodlesServices services, IMoodlesDatabase database)
    {
        Logger = new Logger();
        MoodleFileSystem = new MoodleFileSystem(dalamudServices, this, services, database);
    }

    public void Dispose()
    {
        MoodleFileSystem.Dispose();
    }
}
