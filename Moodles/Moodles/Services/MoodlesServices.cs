using Moodles.Moodles.Mediation;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Services.Wrappers;
using System;

namespace Moodles.Moodles.Services;

internal class MoodlesServices : IMoodlesServices
{
    readonly DalamudServices DalamudServices;

    public Configuration Configuration { get; }

    public IStringHelper StringHelper { get; }
    public ISheets Sheets { get; }
    public IMoodlesMediator Mediator { get; }
    public MediationLogger MediationLogger { get; }
    public IMoodlesCache MoodlesCache { get; }
    public IMoodleValidator MoodleValidator { get; }
    public IMoodlesTargetManager TargetManager { get; }

    public MoodlesServices(DalamudServices dalamudServices, IUserList userList)
    {
        DalamudServices = dalamudServices;

        PluginLog.Initialise(DalamudServices);

        Mediator = new MoodleMediator();
        MediationLogger = new MediationLogger(Mediator);

        Configuration = DalamudServices.DalamudPlugin.GetPluginConfig() as Configuration ?? new Configuration();

        StringHelper = new StringHelperWrapper();
        Sheets = new SheetsWrapper(DalamudServices);
        MoodlesCache = new MoodlesCache(DalamudServices, Sheets);
        MoodleValidator = new MoodleValidator(Sheets);
        TargetManager = new MoodlesTargetManager(dalamudServices, userList);
    }

    public void Dispose()
    {
        MediationLogger.Dispose();
    }
}
