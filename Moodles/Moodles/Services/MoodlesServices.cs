using Moodles.Moodles.Mediation;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Services.Wrappers;

namespace Moodles.Moodles.Services;

internal class MoodlesServices : IMoodlesServices
{
    readonly DalamudServices DalamudServices;

    public Configuration Configuration { get; }

    public IStringHelper StringHelper { get; }
    public ISheets Sheets { get; }
    public IMoodlesMediator Mediator { get; }
    public IMoodlesCache MoodlesCache { get; }
    public IMoodleValidator MoodleValidator { get; }

    public MoodlesServices(DalamudServices dalamudServices)
    {
        DalamudServices = dalamudServices;

        PluginLog.Initialise(DalamudServices);

        Mediator = new MoodleMediator();

        Configuration = DalamudServices.DalamudPlugin.GetPluginConfig() as Configuration ?? new Configuration();

        StringHelper = new StringHelperWrapper();
        Sheets = new SheetsWrapper(DalamudServices);
        MoodlesCache = new MoodlesCache(DalamudServices, Sheets);
        MoodleValidator = new MoodleValidator();
    }
}
