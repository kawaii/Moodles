using Dalamud.Plugin;
using Moodles.Moodles.Hooking;
using Moodles.Moodles.Hooking.Interfaces;
using Moodles.Moodles.MoodleUsers;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.Updating;
using Moodles.Moodles.Updating.Interfaces;
using System.Reflection;

namespace Moodles;

public sealed class MoodlesPlugin : IDalamudPlugin
{
    public readonly string Version;

    readonly DalamudServices DalamudServices;
    readonly IMoodlesServices MoodlesServices;

    readonly IUserList UserList;
    readonly IMoodlesDatabase Database;

    readonly IUpdateHandler UpdateHandler;
    readonly IHookHandler HookHandler;

    public MoodlesPlugin(IDalamudPluginInterface dalamud)
    {
        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown Version";

        DalamudServices = DalamudServices.Create(dalamud, this);
        MoodlesServices = new MoodlesServices(DalamudServices);

        UserList = new UserList();
        Database = new MoodlesDatabase(MoodlesServices);

        HookHandler = new HookHandler(DalamudServices, MoodlesServices, UserList, Database);
        UpdateHandler = new UpdateHandler(DalamudServices, MoodlesServices);
    }

    public void Dispose()
    {
        UpdateHandler.Dispose();
        HookHandler.Dispose();
    }
}