using Dalamud.Plugin;
using Moodles.Moodles.Hooking;
using Moodles.Moodles.Hooking.Interfaces;
using Moodles.Moodles.MoodleUsers;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.OtterGUIHandlers;
using Moodles.Moodles.SaveHandling;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.TempWindowing;
using Moodles.Moodles.Updating;
using Moodles.Moodles.Updating.Interfaces;
using System;
using System.Reflection;

namespace Moodles;

public sealed class MoodlesPlugin : IDalamudPlugin
{
    public readonly string Version;

    readonly DalamudServices DalamudServices;
    readonly IMoodlesServices MoodlesServices;

    readonly SaveHandler SaveHandler;

    readonly IUserList UserList;
    readonly IMoodlesDatabase Database;

    readonly IUpdateHandler UpdateHandler;
    readonly IHookHandler HookHandler;

    readonly OtterGuiHandler OtterGuiHandler;
    readonly WindowHandler WindowHandler;

    readonly MoodleHelper MoodleHelper;

    public MoodlesPlugin(IDalamudPluginInterface dalamud)
    {
        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown Version";

        DalamudServices = DalamudServices.Create(dalamud, this);

        UserList = new UserList();

        MoodlesServices = new MoodlesServices(DalamudServices, UserList);

        SaveHandler = new SaveHandler(MoodlesServices);

        Database = new MoodlesDatabase(MoodlesServices, UserList);

        HookHandler = new HookHandler(DalamudServices, MoodlesServices, UserList, Database);
        UpdateHandler = new UpdateHandler(DalamudServices, MoodlesServices, SaveHandler, Database, UserList);

        MoodleHelper = new MoodleHelper(Database, DalamudServices, MoodlesServices, UserList);

        Database.DatabaseApplier.FloodDatabase();

        OtterGuiHandler = new OtterGuiHandler(DalamudServices, MoodlesServices, Database, UserList);
        WindowHandler = new WindowHandler(DalamudServices, Database, UserList, OtterGuiHandler, MoodlesServices);

        MoodlesServices.Configuration.Initialise(DalamudServices.DalamudPlugin, Database);
    }

    public void Dispose()
    {
        SafeDispose(SaveHandler.ForceSave);

        SafeDispose(UpdateHandler.Dispose);
        SafeDispose(HookHandler.Dispose);
        SafeDispose(WindowHandler.Dispose);
        SafeDispose(OtterGuiHandler.Dispose);
        SafeDispose(MoodlesServices.Dispose);
    }

    void SafeDispose(Action disposeAction)
    {
        try
        {
            disposeAction.Invoke();
        }
        catch (Exception e)
        {
            PluginLog.LogException(e);
        }
    }
}