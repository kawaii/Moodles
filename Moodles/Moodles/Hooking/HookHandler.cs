using Moodles.Moodles.Hooking.Interfaces;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Services;
using System.Collections.Generic;
using System;

namespace Moodles.Moodles.Hooking;

internal class HookHandler : IHookHandler
{
    readonly DalamudServices DalamudServices;
    readonly IMoodlesServices MoodlesServices;

    readonly List<IHookableElement> _hookableElements = new List<IHookableElement>();

    public HookHandler(DalamudServices dalamudServices, IMoodlesServices moodlesServices)
    {
        DalamudServices = dalamudServices;
        MoodlesServices = moodlesServices;

        _Register();
    }

    void _Register()
    {

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
