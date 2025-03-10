using Dalamud.Interface.Windowing;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.OtterGUIHandlers;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.TempWindowing.Windows;
using System;
using System.Collections.Generic;

namespace Moodles.Moodles.TempWindowing;

internal class WindowHandler : IDisposable
{
    static int _internalCounter = 0;
    public static int InternalCounter => _internalCounter++;

    readonly OtterGuiHandler OtterGuiHandler;
    readonly WindowSystem WindowSystem;

    readonly DalamudServices DalamudServices;

    readonly List<MoodleWindow> _windows = new List<MoodleWindow>();

    readonly IMoodlesDatabase Database;
    readonly IUserList UserList;
    readonly IMoodlesServices Services;

    public WindowHandler(DalamudServices dalamudServices, IMoodlesDatabase database, IUserList userList, OtterGuiHandler otterGuiHandler, IMoodlesServices services)
    {
        DalamudServices = dalamudServices;
        Database = database;
        UserList = userList;
        Services = services;

        OtterGuiHandler = otterGuiHandler;

        WindowSystem = new WindowSystem("KiteIsHonestlyKindOfStinkyButSuperCuteAsWell");

        DalamudServices.DalamudPlugin.UiBuilder.Draw += Draw;

        _Register();
    }

    void _Register()
    {
        Register(new MainWindow(OtterGuiHandler, DalamudServices, Services, Database, UserList));
    }

    void Register(MoodleWindow moodleWindow)
    {
        _windows.Add(moodleWindow);
        WindowSystem.AddWindow(moodleWindow);
    }

    void Draw()
    {
        _internalCounter = 0;

        WindowSystem.Draw();
    }

    public void Dispose()
    {
        DalamudServices.DalamudPlugin.UiBuilder.Draw -= Draw;

        WindowSystem.RemoveAllWindows();
    }
}
