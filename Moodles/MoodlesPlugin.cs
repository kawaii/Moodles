using Dalamud.Plugin;
using Moodles.Moodles.Hooking;
using Moodles.Moodles.Hooking.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Updating;
using Moodles.Moodles.Updating.Interfaces;
using System.Reflection;

namespace Moodles;

public sealed class MoodlesPlugin : IDalamudPlugin
{
    public readonly string Version;

    readonly DalamudServices DalamudServices;
    readonly IMoodlesServices MoodlesServices;

    readonly IUpdateHandler UpdateHandler;
    readonly IHookHandler HookHandler;

    public MoodlesPlugin(IDalamudPluginInterface dalamud)
    {
        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown Version";

        DalamudServices = DalamudServices.Create(dalamud, this);
        MoodlesServices = new MoodlesServices(DalamudServices);

        HookHandler = new HookHandler(DalamudServices, MoodlesServices);
        UpdateHandler = new UpdateHandler(DalamudServices, MoodlesServices);
    }

    public void Dispose()
    {
        UpdateHandler.Dispose();
        HookHandler.Dispose();
    }
}