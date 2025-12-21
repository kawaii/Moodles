using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using Moodles.Data;

namespace Moodles;

// Holds the internal IPC data and IPCProcesser logic.
public static unsafe class IPC
{
    // Do not store personal information about IPC players in the config, store them internally during plugin lifetime.
    internal static List<WhitelistEntryGSpeak>      WhitelistGSpeak     = [];
    internal static List<WhitelistEntrySundouleia>  WhitelistSundouleia = [];

    // Hold internal GSpeak & Sundouleia Data.
    public static bool GSpeakAvailable      = false;
    public static bool SundouleiaAvailable  = false;
    public static Dictionary<nint, IPCMoodleAccessTuple> GSpeakPlayerCache      = [];
    public static Dictionary<nint, IPCMoodleAccessTuple> SundouleiaPlayerCache  = [];

    // Faster bitwise check for MoodleAccess than .HasFlag()
    public static bool HasAny(this MoodleAccess flags, MoodleAccess check) => (flags & check) != 0;

    public static void FetchInitial()
    {
        var gSpeak = Svc.PluginInterface.InstalledPlugins.FirstOrDefault(p => string.Equals(p.InternalName, "ProjectGagSpeak", StringComparison.OrdinalIgnoreCase));
        var sundouleia = Svc.PluginInterface.InstalledPlugins.FirstOrDefault(p => string.Equals(p.InternalName, "Sundouleia", StringComparison.OrdinalIgnoreCase));
        GSpeakAvailable = gSpeak is { } gSpeakPlugin && gSpeakPlugin.IsLoaded;
        SundouleiaAvailable = sundouleia is { } sundouleiaPlugin && sundouleiaPlugin.IsLoaded;
        if (GSpeakAvailable)
        {
            InitGSpeakCache();
        }
        if (SundouleiaAvailable)
        {
            InitSundesmoCache();
        }
    }

    #region Sundouleia
    public static void InitSundesmoCache()
    {
        if (P.IPCProcessor.GetAllSundouleiaInfo.TryInvoke(out var allInfo) && allInfo != null)
        {
            SundouleiaPlayerCache = allInfo;
            WhitelistSundouleia.Clear();
            // Add or update existing entries.
            foreach (var (addr, info) in allInfo)
            {
                if (CharaWatcher.Rendered.TryGetValue(addr, out var targetAddr))
                {
                    WhitelistSundouleia.Add(new WhitelistEntrySundouleia(addr, info));
                }
            }
        }
    }

    public static unsafe void AddOrUpdateSundesmo(nint addr, IPCMoodleAccessTuple info)
    {
        PluginLog.Verbose($"Sundouleia Adding/Updating rendered player: {addr:X}");
        SundouleiaPlayerCache[addr] = info;
        // Add or update existing entry.
        if (WhitelistSundouleia.FirstOrDefault(x => x.Address == addr) is { } existing)
        {
            existing.UpdateData(info);
        }
        else
        {
            if (CharaWatcher.Rendered.TryGetValue(addr, out var targetAddr))
            {
                WhitelistSundouleia.Add(new WhitelistEntrySundouleia(addr, info));
            }
        }
    }

    public static void RemoveSundesmo(nint addr)
    {
        SundouleiaPlayerCache.Remove(addr);
        if (WhitelistSundouleia.FirstOrDefault(x => x.Address == addr) is { } existing)
        {
            WhitelistSundouleia.Remove(existing);
        }
    }

    public static void ClearSundesmos()
    {
        SundouleiaPlayerCache.Clear();
        WhitelistSundouleia.Clear();
    }

    public static void SendSundouleiaMessage(this Preset Preset, nint targetAddr)
    {
        if (WhitelistSundouleia.FirstOrDefault(x => x.Address == targetAddr) is not { } entry)
        {
            PluginLog.Error("Target player is not whitelisted for Sundouleia moodles.");
            return;
        }
        // Obtain all MoodlesStatusInfo tuples from the preset status list. If any fail validation, exit.
        var list = new List<MoodlesStatusInfo>();
        foreach (var s in C.SavedStatuses.Where(x => Preset.Statuses.Contains(x.GUID)))
        {
            var preparedStatus = s.PrepareToApply();
            preparedStatus.Applier = Player.NameWithWorld ?? "";
            if (!preparedStatus.IsValid(out var error))
            {
                PluginLog.Error($"Could not apply status: {error}");
            }
            else if (!entry.CanApplyStatus(preparedStatus, out var applyError))
            {
                Notify.Error($"Cannot apply status '{preparedStatus.Title}' to target: {applyError}");
                return; // Exit early if it could not be applied.
            }
            else
            {
                list.Add(preparedStatus.ToStatusTuple());
            }
        }
        if (list.Count > 0)
        {
            if (P.IPCProcessor.SundouleiaTryApplyToPair.TryInvoke(targetAddr, list, false))
            {
                Notify.Info($"Broadcast success");
            }
            else
            {
                Notify.Error("Broadcast failed");
            }
        }
    }

    public static void SendSundouleiaMessage(this MyStatus Status, nint targetAddr)
    {
        if (WhitelistSundouleia.FirstOrDefault(x => x.Address == targetAddr) is not { } entry)
        {
            PluginLog.Error("Target player is not whitelisted for Sundouleia moodles.");
            return;
        }

        var preparedStatus = Status.PrepareToApply();
        preparedStatus.Applier = LocalPlayer.NameWithWorld ?? string.Empty;
        if (!preparedStatus.IsValid(out var error))
        {
            Notify.Error($"Could not apply status: {error}");
        }
        else if (!entry.CanApplyStatus(preparedStatus, out var applyError))
        {
            Notify.Error($"Cannot apply status '{preparedStatus.Title}' to target: {applyError}");
        }
        else
        {
            if (P.IPCProcessor.SundouleiaTryApplyToPair.TryInvoke(targetAddr, [preparedStatus.ToStatusTuple()], true))
            {
                Notify.Info($"Broadcast success");
            }
            else
            {
                Notify.Error("Broadcast failed");
            }
        }
    }
    #endregion Sundouleia

    #region GSpeak
    public static void InitGSpeakCache()
    {
        if (P.IPCProcessor.GetAllGSpeakInfo.TryInvoke(out var allInfo) && allInfo != null)
        {
            GSpeakPlayerCache = allInfo;
            WhitelistGSpeak.Clear();
            // Add or update existing entries.
            foreach (var (addr, info) in allInfo)
            {
                if (CharaWatcher.Rendered.TryGetValue(addr, out var targetAddr))
                {
                    WhitelistGSpeak.Add(new WhitelistEntryGSpeak(addr, info));
                }
            }
        }
    }

    public static void AddOrUpdateGSpeakPair(nint addr, IPCMoodleAccessTuple info)
    {
        PluginLog.Verbose($"GSpeak access Added/Updated for address: {addr:X}");

        GSpeakPlayerCache[addr] = info;
        // Add or update existing entry.
        if (WhitelistGSpeak.FirstOrDefault(x => x.Address == addr) is { } existing)
        {
            existing.UpdateData(info);
        }
        else
        {
            if (CharaWatcher.Rendered.TryGetValue(addr, out var targetAddr))
            {
                WhitelistGSpeak.Add(new WhitelistEntryGSpeak(addr, info));
            }
        }
    }

    public static void RemoveGSpeakPair(nint addr)
    {
        GSpeakPlayerCache.Remove(addr);
        if (WhitelistGSpeak.FirstOrDefault(x => x.Address == addr) is { } existing)
        {
            WhitelistGSpeak.Remove(existing);
        }
    }

    public static void ClearGSpeakPairs()
    {
        GSpeakPlayerCache.Clear();
        WhitelistGSpeak.Clear();
    }

    public static void SendGSpeakMessage(this Preset Preset, nint targetAddr)
    {
        if (WhitelistGSpeak.FirstOrDefault(x => x.Address == targetAddr) is not { } entry)
        {
            PluginLog.Error("Target player is not whitelisted for GSpeak moodles.");
            return;
        }
        // Obtain all MoodlesStatusInfo tuples from the preset status list. If any fail validation, exit.
        var list = new List<MoodlesStatusInfo>();
        foreach (var s in C.SavedStatuses.Where(x => Preset.Statuses.Contains(x.GUID)))
        {
            var preparedStatus = s.PrepareToApply();
            preparedStatus.Applier = LocalPlayer.NameWithWorld ?? string.Empty;
            if (!preparedStatus.IsValid(out var error))
            {
                PluginLog.Error($"Could not apply status: {error}");
            }
            else if (!entry.CanApplyStatus(preparedStatus, out var applyError))
            {
                Notify.Error($"Cannot apply status '{preparedStatus.Title}' to target: {applyError}");
                return; // Exit early if it could not be applied.
            }
            else
            {
                list.Add(preparedStatus.ToStatusTuple());
            }
        }
        if (list.Count > 0)
        {
            if (P.IPCProcessor.GSpeakTryApplyToPair.TryInvoke(targetAddr, list, false))
            {
                Notify.Info($"Broadcast success");
            }
            else
            {
                Notify.Error("Broadcast failed");
            }
        }
    }

    public static unsafe void SendGSpeakMessage(this MyStatus Status, nint targetAddr)
    {
        if (WhitelistGSpeak.FirstOrDefault(x => x.Address == targetAddr) is not { } entry)
        {
            PluginLog.Error("Target player is not whitelisted for GSpeak moodles.");
            return;
        }

        var preparedStatus = Status.PrepareToApply();
        preparedStatus.Applier = Player.NameWithWorld ?? "";
        if (!preparedStatus.IsValid(out var error))
        {
            Notify.Error($"Could not apply status: {error}");
        }
        else if (!entry.CanApplyStatus(preparedStatus, out var applyError))
        {
            Notify.Error($"Cannot apply status '{preparedStatus.Title}' to target: {applyError}");
        }
        else
        {
            if (P.IPCProcessor.GSpeakTryApplyToPair.TryInvoke(targetAddr, [preparedStatus.ToStatusTuple()], true))
            {
                Notify.Info($"Broadcast success");
            }
            else
            {
                Notify.Error("Broadcast failed");
            }
        }
    }
    #endregion GSpeak
}
