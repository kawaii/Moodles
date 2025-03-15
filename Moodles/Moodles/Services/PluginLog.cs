using Dalamud.Plugin.Services;
using System;

namespace Moodles.Moodles.Services;

internal static class PluginLog
{
    static IPluginLog? Logger;
    static bool AllowLogging = true;

    public static void Initialise(DalamudServices dalamudServices)
    {
        Logger = dalamudServices.PluginLog;
    }

    public static void Log(object? message)
    {
        if (!AllowLogging) return;
        if (message == null) return;
        Logger?.Debug($"{message}");
    }

    public static void LogError(Exception e, object? message)
    {
        if (!AllowLogging) return;
        if (message == null) return;
        Logger?.Error($"{e} : {message}");
    }

    public static void LogException(Exception e)
    {
        Logger?.Error($"{e}");
    }

    public static void LogFatal(object? message)
    {
        if (!AllowLogging) return;
        if (message == null) return;
        Logger?.Fatal($"{message}");
    }

    public static void LogInfo(object? message)
    {
        if (!AllowLogging) return;
        if (message == null) return;
        Logger?.Info($"{message}");
    }

    public static void LogVerbose(object? message)
    {
        if (!AllowLogging) return;
        if (message == null) return;
        Logger?.Verbose($"{message}");
    }

    public static void LogWarning(object? message)
    {
        if (!AllowLogging) return;
        if (message == null) return;
        Logger?.Warning($"{message}");
    }
}
