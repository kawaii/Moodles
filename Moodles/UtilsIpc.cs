using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using Moodles.Data;
using Status = Lumina.Excel.Sheets.Status;

namespace Moodles;
public static unsafe partial class Utils
{
    // Faster bitwise check for MoodleAccess than .HasFlag()
    public static bool HasAny(this MoodleAccess flags, MoodleAccess check) => (flags & check) != 0;

    public static void FetchInitialIpcInfo()
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


    // Pointers are retained by both IPC's via monitoring Object Initialize and Destroy calls, 
    // and will inform moodles when an update occured to avoid per-frame checks.
    #region Sundouleia
    public static bool SundouleiaAvailable = false;
    public static Dictionary<nint, IPCMoodleAccessTuple> SundouleiaPlayerCache = [];
    public static void InitSundesmoCache()
    {
        if (P.IPCProcessor.GetAllSundouleiaInfo.TryInvoke(out var allInfo) && allInfo != null)
        {
            SundouleiaPlayerCache = allInfo;
            C.WhitelistSundouleia.Clear();
            // Add or update existing entries.
            foreach (var (addr, info) in allInfo)
            {
                if (Svc.Objects.CreateObjectReference(addr) is IPlayerCharacter pc)
                {
                    C.WhitelistSundouleia.Add(new WhitelistEntrySundouleia(addr, pc.GetNameWithWorld(), info));
                }
            }
        }
    }

    public static void AddSundesmo(nint addr, IPCMoodleAccessTuple info)
    {
        SundouleiaPlayerCache[addr] = info;
        // Add or update existing entry.
        if (C.WhitelistSundouleia.FirstOrDefault(x => x.Address == addr) is { } existing)
        {
            existing.UpdateData(info);
        }
        else
        {
            if (Svc.Objects.CreateObjectReference(addr) is IPlayerCharacter pc)
            {
                C.WhitelistSundouleia.Add(new WhitelistEntrySundouleia(addr, pc.GetNameWithWorld(), info));
            }
        }
    }

    public static void RemoveSundesmo(nint addr)
    {
        SundouleiaPlayerCache.Remove(addr);
        if (C.WhitelistSundouleia.FirstOrDefault(x => x.Address == addr) is { } existing)
        {
            C.WhitelistSundouleia.Remove(existing);
        }
    }

    public static void ClearSundesmos()
    {
        SundouleiaPlayerCache.Clear();
        C.WhitelistSundouleia.Clear();
    }

    public static void SendSundouleiaMessage(this Preset Preset, IPlayerCharacter target)
    {
        if (C.WhitelistSundouleia.FirstOrDefault(x => x.Address == target.Address) is not { } entry)
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
                list.Add(preparedStatus.ToStatusInfoTuple());
            }
        }
        if (list.Count > 0)
        {
            if (P.IPCProcessor.SundouleiaTryApplyToPair.TryInvoke(target.Address, list, false))
            {
                Notify.Info($"Broadcast success");
            }
            else
            {
                Notify.Error("Broadcast failed");
            }
        }
    }

    public static void SendSundouleiaMessage(this MyStatus Status, IPlayerCharacter target)
    {
        if (C.WhitelistSundouleia.FirstOrDefault(x => x.Address == target.Address) is not { } entry)
        {
            PluginLog.Error("Target player is not whitelisted for Sundouleia moodles.");
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
            if (P.IPCProcessor.SundouleiaTryApplyToPair.TryInvoke(target.Address, [preparedStatus.ToStatusInfoTuple()], true))
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
    public static bool GSpeakAvailable = false;
    public static Dictionary<nint, IPCMoodleAccessTuple> GSpeakPlayerCache = [];
    public static void InitGSpeakCache()
    {
        if (P.IPCProcessor.GetAllGSpeakInfo.TryInvoke(out var allInfo) && allInfo != null)
        {
            GSpeakPlayerCache = allInfo;
            C.WhitelistGSpeak.Clear();
            // Add or update existing entries.
            foreach (var (addr, info) in allInfo)
            {
                if (Svc.Objects.CreateObjectReference(addr) is IPlayerCharacter pc)
                {
                    C.WhitelistGSpeak.Add(new WhitelistEntryGSpeak(addr, pc.GetNameWithWorld(), info));
                }
            }
        }
    }

    public static void AddGSpeakPair(nint addr, IPCMoodleAccessTuple info)
    {
        GSpeakPlayerCache[addr] = info;
        // Add or update existing entry.
        if (C.WhitelistGSpeak.FirstOrDefault(x => x.Address == addr) is { } existing)
        {
            existing.UpdateData(info);
        }
        else
        {
            if (Svc.Objects.CreateObjectReference(addr) is IPlayerCharacter pc)
            {
                C.WhitelistGSpeak.Add(new WhitelistEntryGSpeak(addr, pc.GetNameWithWorld(), info));
            }
        }
    }

    public static void RemoveGSpeakPair(nint addr)
    {
        GSpeakPlayerCache.Remove(addr);
        if (C.WhitelistGSpeak.FirstOrDefault(x => x.Address == addr) is { } existing)
        {
            C.WhitelistGSpeak.Remove(existing);
        }
    }

    public static void ClearGSpeakPairs()
    {
        GSpeakPlayerCache.Clear();
        C.WhitelistGSpeak.Clear();
    }

    public static void SendGSpeakMessage(this Preset Preset, IPlayerCharacter target)
    {
        if (C.WhitelistGSpeak.FirstOrDefault(x => x.Address == target.Address) is not { } entry)
        {
            PluginLog.Error("Target player is not whitelisted for GSpeak moodles.");
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
                list.Add(preparedStatus.ToStatusInfoTuple());
            }
        }
        if (list.Count > 0)
        {
            if (P.IPCProcessor.GSpeakTryApplyToPair.TryInvoke(target.Address, list, false))
            {
                Notify.Info($"Broadcast success");
            }
            else
            {
                Notify.Error("Broadcast failed");
            }
        }
    }

    public static void SendGSpeakMessage(this MyStatus Status, IPlayerCharacter target)
    {
        if (C.WhitelistGSpeak.FirstOrDefault(x => x.Address == target.Address) is not { } entry)
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
            if (P.IPCProcessor.GSpeakTryApplyToPair.TryInvoke(target.Address, [preparedStatus.ToStatusInfoTuple()], true))
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
