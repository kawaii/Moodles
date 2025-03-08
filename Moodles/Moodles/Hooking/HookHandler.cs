using Moodles.Moodles.Hooking.Interfaces;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Services;
using System.Collections.Generic;
using System;
using Moodles.Moodles.Hooking.Hooks;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.Hooking;

internal class HookHandler : IHookHandler
{
    readonly DalamudServices DalamudServices;
    readonly IMoodlesServices MoodlesServices;
    readonly IUserList UserList;
    readonly IMoodlesDatabase Database;

    readonly List<IHookableElement> _hookableElements = new List<IHookableElement>();

    public HookHandler(DalamudServices dalamudServices, IMoodlesServices moodlesServices, IUserList userList, IMoodlesDatabase database)
    {
        DalamudServices = dalamudServices;
        MoodlesServices = moodlesServices;
        UserList = userList;
        Database = database;

        _Register();
    }

    void _Register()
    {
        Register(new CharacterManagerHook(DalamudServices, UserList, MoodlesServices, Database));
    }

    void Register(IHookableElement hookableElement)
    {
        _hookableElements.Add(hookableElement);
        hookableElement.Init();
    }

    public void Dispose()
    {
        foreach (IHookableElement hookableElement in _hookableElements)
        {
            try
            {
                hookableElement.Dispose();
            }
            catch(Exception e)
            {
                PluginLog.LogException(e);
            }
        }
    }
}
