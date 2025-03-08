using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Services.Wrappers;

namespace Moodles.Moodles.Services;

internal class MoodlesServices : IMoodlesServices
{
    readonly DalamudServices DalamudServices;

    public Configuration Configuration { get; }

    public IStringHelper StringHelper { get; }
    public ISheets Sheets { get; }

    public MoodlesServices(DalamudServices dalamudServices)
    {
        DalamudServices = dalamudServices;

        PluginLog.Initialise(DalamudServices);

        Configuration = DalamudServices.DalamudPlugin.GetPluginConfig() as Configuration ?? new Configuration();
        StringHelper = new StringHelperWrapper();
        Sheets = new SheetsWrapper(DalamudServices);
    }
}
