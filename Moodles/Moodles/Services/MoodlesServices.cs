using Moodles.Moodles.Services.Interfaces;
using OtterGui.Services;

namespace Moodles.Moodles.Services;

internal class MoodlesServices : IMoodlesServices
{
    readonly DalamudServices DalamudServices;

    public Configuration Configuration { get; }

    public MoodlesServices(DalamudServices dalamudServices)
    {
        DalamudServices = dalamudServices;

        PluginLog.Initialise(DalamudServices);

        Configuration = DalamudServices.DalamudPlugin.GetPluginConfig() as Configuration ?? new Configuration();
    }
}
