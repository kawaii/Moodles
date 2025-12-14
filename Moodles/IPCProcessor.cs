using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using Moodles.Data;
using System.Collections.Immutable;

namespace Moodles;
public class IPCProcessor : IDisposable
{
    #region Moodles Events
    [EzIPCEvent] private readonly Action Ready;
    [EzIPCEvent] private readonly Action Unloading;

    /// <summary>
    ///     Triggered when a monitored player's Status Manager is changes.
    /// </summary>
    [EzIPCEvent] public readonly Action<IPlayerCharacter> StatusManagerModified;

    /// <summary>
    ///     Triggered when a <see cref="MyStatus"/> is updated, added, or removed. (2nd parameter indicates removal)
    /// </summary>
    [EzIPCEvent] public readonly Action<Guid, bool> StatusUpdated;

    /// <summary>
    ///     Triggered when a <see cref="Preset"/> is updated, added, or removed. (2nd parameter indicates removal)
    /// </summary>
    [EzIPCEvent] public readonly Action<Guid, bool> PresetUpdated;
    #endregion Moodles Events

    #region GSpeak & Sundouleia Getters
    /// <summary> Gets all handled addresses managed by the IPC Source. (Could do name string if ptr is more annoying) </summary>
    [EzIPC("Sundouleia.GetAllRendered", false)] public readonly Func<List<nint>> GetSundouleiaPlayers;

    /// <inheritdoc cref="GetSundouleiaPlayers"/>
    [EzIPC("GagSpeak.GetAllRendered", false)] public readonly Func<List<nint>> GetGSpeakPlayers;


    /// <summary> Get the KVP's of handles addresses with their <see cref="IPCMoodleAccessTuple"/> (Usually on initialization)./> </summary>
    [EzIPC("Sundouleia.GetAllRenderedInfo", false)] public readonly Func<Dictionary<nint, IPCMoodleAccessTuple>> GetAllSundouleiaInfo;

    /// <inheritdoc cref="GetSundouleiaAccessPerms"/>
    [EzIPC("GagSpeak.GetAllRenderedInfo", false)] public readonly Func<Dictionary<nint, IPCMoodleAccessTuple>> GetAllGSpeakInfo;


    /// <summary> Get the <see cref="IPCMoodleAccessTuple"/> for a specific handled address. </summary>
    [EzIPC("Sundouleia.GetAccessInfo", false)] public readonly Func<nint, IPCMoodleAccessTuple> GetSundouleiaAccessInfo;

    /// <inheritdoc cref="GetSundouleiaAccessInfo"/>
    [EzIPC("GagSpeak.GetAccessInfo", false)] public readonly Func<nint, IPCMoodleAccessTuple> GetGSpeakAccessInfo;
    #endregion GSpeak & Sundouleia Getters

    #region GSpeak & Sundouleia Listener Events
    // Broadcasts to the IPC to apply the statuses to the pair.
    [EzIPC("Sundouleia.ApplyToPairRequest", false)] public readonly Action<nint, List<MoodlesStatusInfo>, bool> GSpeakTryApplyToPair;
    [EzIPC("GagSpeak.ApplyToPairRequest", false)] public readonly Action<nint, List<MoodlesStatusInfo>, bool> SundouleiaTryApplyToPair;


    [EzIPCEvent("Sundouleia.Ready", false)]
    private static void SundouleiaReady()
    {
        new TickScheduler(() =>
        {
            PluginLog.LogDebug("GSpeak Ready, Obtaining all handled player information.");
            Utils.SundouleiaAvailable = true;
            Utils.InitSundesmoCache();
        });
    }

    [EzIPCEvent("GagSpeak.Ready", false)]
    private static void GSpeakReady()
    {
        new TickScheduler(() =>
        {
            PluginLog.LogDebug("GSpeak Ready, Obtaining all handled player information.");
            Utils.GSpeakAvailable = true;
            Utils.InitGSpeakCache();
        });
    }

    [EzIPCEvent("Sundouleia.Disposing", false)]
    private static void SundouleiaDisposing()
    {
        PluginLog.LogDebug("Sundouleia Disposed / Disabled. Clearing associated data.");
        Utils.ClearSundesmos();
        Utils.SundouleiaAvailable = false;
    }

    [EzIPCEvent("GagSpeak.Disposing", false)] 
    private static void GSpeakDisposing()
    {
        PluginLog.LogDebug("GSpeak Disposed / Disabled. Clearing associated data.");
        Utils.ClearGSpeakPairs();
        Utils.GSpeakAvailable = false;
    }

    [EzIPCEvent("Sundouleia.PairRendered", false)]
    private void SundouleiaPairRendered(nint address)
    {
        PluginLog.LogDebug($"Sundouleia handling new rendered player: {address:X}");
        Utils.AddSundesmo(address, GetSundouleiaAccessInfo(address));
    }

    [EzIPCEvent("GagSpeak.PairRendered", false)]
    private void GSpeakPairRendered(nint address)
    {
        PluginLog.LogDebug($"GSpeak handling new rendered player: {address:X}");
        Utils.AddGSpeakPair(address, GetGSpeakAccessInfo(address));
    }

    [EzIPCEvent("Sundouleia.PairUnrendered", false)]
    private static void SundouleiaPairUnrendered(nint address)
    {
        PluginLog.LogDebug($"Sundouleia removing unrendered player: {address:X}");
        Utils.RemoveSundesmo(address);
    }

    [EzIPCEvent("GagSpeak.PairUnrendered", false)] 
    private static void GSpeakPairUnrendered(nint address)
    {
        PluginLog.LogDebug($"GSpeak removing unrendered player: {address:X}");
        Utils.RemoveGSpeakPair(address);
    }

    /// <summary>
    ///     Invoked whenever the IpcMoodleAccessTuple is updated for <paramref name="address"/>.
    ///     Should obtain updated IpcMoodleAccessTuple for the address to stay updated.
    /// </summary>
    /// <remarks> <b>Returned tuple is always in order of (CLIENT, PAIR(address))</b></remarks>
    [EzIPCEvent("Sundouleia.AccessUpdated", false)]
    private static void SundouleiaAccessUpdated(nint address)
    {
        // Can be done via utils
    }

    /// <inheritdoc cref="SundouleiaAccessUpdated(nint)"/>
    [EzIPCEvent("GagSpeak.AccessUpdated", false)]
    private static void GSpeakAccessUpdated(nint address)
    {
        // Can be done via utils
    }

    [EzIPCEvent("Sundouleia.ApplyStatusInfo", false)]
    private static void SundouleiaApplyTuple(MoodlesStatusInfo status)
    {
        new TickScheduler(() => ApplyStatusTuples([ status ]));
    }

    /// <inheritdoc cref="ApplyStatusTuples(List{MoodlesStatusInfo})"/>
    [EzIPCEvent("GagSpeak.ApplyStatusInfo", false)]
    private static void GSpeakApplyTuple(MoodlesStatusInfo status)
    {
        new TickScheduler(() => ApplyStatusTuples([ status ]));
    }

    /// <inheritdoc cref="ApplyStatusTuples(List{MoodlesStatusInfo})"/>
    [EzIPCEvent("Sundouleia.ApplyStatusInfoList", false)]
    private static void SundouleiaApplyTuples(List<MoodlesStatusInfo> statuses)
    {
        new TickScheduler(() => ApplyStatusTuples(statuses));
    }

    /// <inheritdoc cref="ApplyStatusTuples(List{MoodlesStatusInfo})"/>
    [EzIPCEvent("GagSpeak.ApplyStatusInfoList", false)]
    private static void GSpeakApplyTuples(List<MoodlesStatusInfo> statuses)
    {
        new TickScheduler(() => ApplyStatusTuples(statuses));
    }

    /// <summary>
    ///     <b>Primarily used for Apply-To-Pair functionality, or for Try-On features.</b> <para />
    ///     By the time this method is called, any pair-applied tuples have been validated by 
    ///     GSpeak for valid MoodleAccess and can be trusted.
    /// </summary>
    /// <remarks> Ensure this is called in a <see cref="TickScheduler"/> </remarks>
    private static void ApplyStatusTuples(List<MoodlesStatusInfo> tuples)
    {
        if (Player.Object is null) return;
        PluginLog.LogDebug($"Applying statuses: ({string.Join(",", tuples.Select(s => s.Title))})");
        var sm = Utils.GetMyStatusManager(Player.Object);
        foreach (var status in tuples)
        {
            sm.AddOrUpdate(MyStatus.FromTuple(status).PrepareToApply(), UpdateSource.StatusTuple, false, true);
        }
    }
    #endregion GSpeak & Sundouleia Listener Events

    public IPCProcessor()
    {
        EzIPC.Init(this);
        Ready();
    }

    public void Dispose()
    {
        Unloading();
    }

    [EzIPC]
    private int Version()
    {
        return 3;
    }

    #region StatusManager
    /// <summary> 
    ///     Attempts to clear the active Moodles on a player using their name. <para />
    ///     Does not complete if a player by this name is not found within the object table.
    /// </summary>
    [EzIPC("ClearStatusManagerByNameV2")]
    private void ClearStatusManager(string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return;
        ClearStatusManager((IPlayerCharacter)obj);
    }

    /// <summary> 
    ///     Attempts to clear the active Moodles on a player using the objects address. <para />
    ///     This address is used to obtain a IPlayerCharacter object reference.
    /// </summary>
    [EzIPC("ClearStatusManagerByPtrV2")]
    private void ClearStatusManager(nint ptr)
    {
        if (ptr == nint.Zero)
        {
            PluginLog.LogWarning("[IPC] Clear Status Manager Ptr is 0");
            return;
        }

        var gameObjectReference = Svc.Objects.CreateObjectReference(ptr);
        if (gameObjectReference == null)
        {
            PluginLog.LogWarning("[IPC] Clear Status Manager Game Object Reference is NULL");
            return;
        }

        var ipc = (IPlayerCharacter)gameObjectReference;
        ClearStatusManager(ipc);
    }

    /// <summary> 
    ///     Attempts to clear the active Moodles on a player using the IPlayerCharacter object reference. <para /> 
    ///     If the object is null or not present, this method will do nothing.
    /// </summary>
    [EzIPC("ClearStatusManagerByPlayerV2")]
    private void ClearStatusManager(IPlayerCharacter pc)
    {
        if (pc == null)
        {
            PluginLog.LogWarning("[IPC] Clear Status Manager PC is NULL");
            return;
        }

        var m = pc.GetMyStatusManager();
        foreach(var s in m.Statuses)
        {
            if(!s.Persistent)
            {
                m.Cancel(s);
            }
        }
        m.Ephemeral = false;
    }


    /// <summary> 
    ///     Attempts to apply the encoded base64 status manager data to a visible player character object by their name. <para />
    ///     Does not complete if a player by this name is not found within the object table or the data is invalid.
    /// </summary>
    [EzIPC("SetStatusManagerByNameV2")]
    private void SetStatusManager(string name, string data)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return;
        SetStatusManager((IPlayerCharacter)obj, data);
    }


    /// <summary> 
    ///     Attempts to apply the encoded base64 status manager data to a visible player character object by their address. <para />
    ///     This address is used to obtain a IPlayerCharacter object reference, and is null if address is not in the object table.
    /// </summary>
    [EzIPC("SetStatusManagerByPtrV2")]
    private void SetStatusManager(nint ptr, string data) => SetStatusManager((IPlayerCharacter)Svc.Objects.CreateObjectReference(ptr), data);


    /// <summary> 
    ///     Attempts to apply the encoded base64 status manager data to a visible player character object by the object reference. <para />
    ///     If the object is null or not present, this method will do nothing.
    /// </summary>
    [EzIPC("SetStatusManagerByPlayerV2")]
    private void SetStatusManager(IPlayerCharacter pc, string data)
    {
        pc.GetMyStatusManager().Apply(data);
    }


    /// <summary>
    ///     Fetches the current status manager data of the client's player character.
    /// </summary>
    /// <returns> The base64 encoded status manager data of the player character. </returns>
    [EzIPC("GetClientStatusManagerV2")]
    private string GetStatusManager() => GetStatusManager(Player.Object);


    /// <summary>
    ///     Fetches the current status manager data of a player character by their name.
    /// </summary>
    /// <returns> The base64 encoded status manager data of the player character. </returns>
    [EzIPC("GetStatusManagerByNameV2")]
    private string GetStatusManager(string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return null;
        return GetStatusManager((IPlayerCharacter)obj);
    }


    /// <summary>
    ///     Fetches the current status manager data of a player character by their address.
    /// </summary>
    /// <returns> The base64 encoded status manager data of the player character. </returns>
    [EzIPC("GetStatusManagerByPtrV2")]
    private string GetStatusManager(nint ptr) => GetStatusManager((IPlayerCharacter)Svc.Objects.CreateObjectReference(ptr));


    /// <summary>
    ///     Fetches the current status manager data of a player character by the IPlayerCharacter object reference.
    /// </summary>
    /// <returns> The base64 encoded status manager data of the player character. </returns>
    [EzIPC("GetStatusManagerByPlayerV2")]
    private string GetStatusManager(IPlayerCharacter pc)
    {
        if(pc == null) return null;
        return pc.GetMyStatusManager().SerializeToBase64();
    }

    /// <summary>
    ///     Fetches the Info of the Statuses active on the Client Player's StatusManager.
    /// </summary>
    /// <returns> The status info tuple of the Statuses Active in the StatusManager. </returns>
    [EzIPC("GetClientStatusManagerInfoV2")]
    private List<MoodlesStatusInfo> GetClientStatusManagerInfo() => GetStatusManagerInfo(Player.Object);

    /// <summary>
    ///     Fetches the Info of the Statuses active on the StatusManager associated with the name.
    /// </summary>
    /// <returns> The status info tuple of the Statuses Active in the StatusManager. </returns>
    [EzIPC("GetStatusManagerInfoByNameV2")]
    private List<MoodlesStatusInfo> GetStatusManagerInfo(string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return new List<MoodlesStatusInfo>();
        return GetStatusManagerInfo((IPlayerCharacter)obj);
    }


    /// <summary>
    ///     Fetches the Info of the Statuses active on the StatusManager associated with the Player Address.
    /// </summary>
    /// <returns> The status info tuple of the Statuses Active in the StatusManager. </returns>
    [EzIPC("GetStatusManagerInfoByPtrV2")]
    private List<MoodlesStatusInfo> GetStatusManagerInfo(nint ptr) => GetStatusManagerInfo((IPlayerCharacter)Svc.Objects.CreateObjectReference(ptr));


    /// <summary>
    ///     Fetches the current status manager data of a player character by the IPlayerCharacter object reference.
    /// </summary>
    /// <returns> The base64 encoded status manager data of the player character. </returns>
    [EzIPC("GetStatusManagerInfoByPlayerV2")]
    private List<MoodlesStatusInfo> GetStatusManagerInfo(IPlayerCharacter pc)
    {
        if(pc == null) return new List<MoodlesStatusInfo>();
        return pc.GetMyStatusManager().GetActiveStatusInfo();
    }
    #endregion StatusManager

    #region MoodlesInfoFetch
    /// <summary>
    ///     Fetches a client's Moodle Status information by the GUID of the Moodle.
    /// </summary>
    /// <returns> The status info tuple of the moodle if it exists, otherwise a default tuple. </returns>
    [EzIPC]
    private MoodlesStatusInfo GetStatusInfoV2(Guid guid)
    {
        if(C.SavedStatuses.TryGetFirst(x => x.GUID == guid, out var status))
        {
            return status.ToStatusInfoTuple();
        }
        return default;
    }

    /// <summary>
    ///     Fetches all the clients Moodle Statuses, and compiles them into Tuple format.
    /// </summary>
    /// <returns> A list of status info tuples containing the moodle information. </returns>
    [EzIPC]
    private List<MoodlesStatusInfo> GetStatusInfoListV2()
    {
        var ret = new List<MoodlesStatusInfo>();
        foreach(var x in C.SavedStatuses)
        {
            ret.Add(x.ToStatusInfoTuple());
        }
        return ret;
    }

    /// <summary>
    ///     Fetches a client's Preset Profile information by the GUID of the Profile.
    /// </summary>
    /// <returns> The profile info tuple of the profile if it exists, otherwise a default tuple. </returns>
    [EzIPC]
    private MoodlePresetInfo GetPresetInfoV2(Guid guid)
    {
        // Return the preset info tuple if invalid.
        if(C.SavedPresets.TryGetFirst(x => x.GUID == guid, out var preset))
        {
            return preset.ToPresetInfoTuple();
        }

        // return empty preset if invalid.
        return new MoodlePresetInfo(Guid.Empty, new List<Guid>(), PresetApplicationType.UpdateExisting, "");
    }

    /// <summary>
    ///     Fetches all the clients Preset Profiles, and compiles them into Tuple format. <para />
    ///     Consider turning the list of status info's into simply a list of GUID, and require 
    ///     other plugins to fetch status list first.
    /// </summary>
    /// <returns> A list of profile info tuples containing the profile information. </returns>
    [EzIPC]
    private List<MoodlePresetInfo> GetPresetsInfoListV2()
    {
        var ret = new List<MoodlePresetInfo>();
        foreach(var x in C.SavedPresets)
        {
            ret.Add(x.ToPresetInfoTuple());
        }
        return ret;
    }

    /// <summary>
    ///     Obtains the list of registered moodles in a shorted struct with only basic information and GUID's for the moodles.
    /// </summary>
    /// <returns> A list of MoodleInfo structs containing the GUID, IconID, Path, and Title of the moodles. </returns>
    [EzIPC]
    private List<MoodlesMoodleInfo> GetRegisteredMoodlesV2()
    {
        var ret = new List<MoodlesMoodleInfo>();
        foreach(var x in C.SavedStatuses)
        {
            if(P.OtterGuiHandler.MoodleFileSystem.FindLeaf(x, out var path))
                ret.Add((x.GUID, (uint)x.IconID, path.FullName(), x.Title));
        }
        return ret;
    }

    /// <summary>
    ///     Obtains the list of registered presets in a shorted struct with only basic information and GUID's for the presets.
    /// </summary>
    /// <returns> A list of ProfileInfo structs containing the GUID and FullPath of the presets. </returns>
    [EzIPC]
    private List<MoodlesProfileInfo> GetRegisteredProfilesV2()
    {
        var ret = new List<MoodlesProfileInfo>();
        foreach(var x in C.SavedPresets)
        {
            if (P.OtterGuiHandler.PresetFileSystem.FindLeaf(x, out var path))
            {
                ret.Add((x.GUID, path.FullName()));
            }
        }
        return ret;
    }
    #endregion MoodlesInfoFetch

    #region MoodlesUpdateManager
    /// <summary>
    ///     Appends a Status from the client's Status List to the status manager of a visible player.
    /// </summary>
    [EzIPC]
    private void AddOrUpdateStatusByNameV2(Guid guid, string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return;
        AddOrUpdateMoodleByPlayerV2(guid, (IPlayerCharacter)obj);
    }

    /// <summary>
    ///     Appends a Preset from the client's Preset List to the status manager of a visible player.
    /// </summary>
    [EzIPC]
    private void AddOrUpdateMoodleByPlayerV2(Guid guid, IPlayerCharacter pc)
    {
        if(C.SavedStatuses.TryGetFirst(x => x.GUID == guid, out var status))
        {
            var sm = pc.GetMyStatusManager();
            if(!sm.Ephemeral)
            {
                PluginLog.LogDebug($"Adding or Updating Moodle {status.Title} to {pc.GetNameWithWorld()}");
                sm.AddOrUpdate(status.PrepareToApply(), UpdateSource.StatusTuple, false, true);
            }
        }
    }


    /// <summary>
    ///     Appends a Preset from the client's Preset List to the status manager of a visible player.
    /// </summary>
    [EzIPC]
    private void ApplyPresetByNameV2(Guid guid, string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return;
        ApplyPresetByPlayerV2(guid, (IPlayerCharacter)obj);
    }

    /// <summary>
    ///     Appends a Preset from the client's Preset List to the status manager of a visible player.
    /// </summary>
    [EzIPC]
    private void ApplyPresetByPlayerV2(Guid guid, IPlayerCharacter pc)
    {
        if(C.SavedPresets.TryGetFirst(x => x.GUID == guid, out var preset))
        {
            var sm = pc.GetMyStatusManager();
            if(!sm.Ephemeral)
            {
                sm.ApplyPreset(preset);
            }
        }
    }

    /// <summary>
    ///     Removes a Status from the client's Status List from the status manager of a visible player.
    /// </summary>
    [EzIPC]
    private void RemoveMoodleByPlayerV2(Guid guid, IPlayerCharacter pc)
    {
        var sm = pc.GetMyStatusManager();
        if (sm.Statuses.TryGetFirst(x => x.GUID == guid, out var status))
        {
            if(!sm.Ephemeral)
            {
                if(!status.Persistent)
                {
                    sm.Cancel(guid);
                }
            }
        }
    }

    /// <summary>
    ///     Removes a Status from the a visible players active status manager.
    /// </summary>
    [EzIPC]
    private void RemoveMoodlesByNameV2(List<Guid> guids, string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return;
        RemoveMoodlesByPlayerV2(guids, (IPlayerCharacter)obj);
    }

    /// <summary>
    ///     Removes a list of Statuses from the a visible players active status manager.
    /// </summary>
    [EzIPC]
    private void RemoveMoodlesByPlayerV2(List<Guid> guids, IPlayerCharacter pc)
    {
        var sm = pc.GetMyStatusManager();
        foreach(var guid in guids)
        {
            if(sm.Statuses.TryGetFirst(x => x.GUID == guid, out var status))
            {
                if(!sm.Ephemeral)
                {
                     if (!status.Persistent)
                     {
                         sm.Cancel(guid);
                     }
                }
            }
        }
    }

    /// <summary>
    ///     Removes a list of Statuses contained in the preset GUID from the a visible players active status manager.
    /// </summary>
    [EzIPC]
    private void RemovePresetByPlayerV2(Guid guid, IPlayerCharacter pc)
    {
        // preset must exist in our saved presets since presets are not stored in the Status Manager
        if(C.SavedPresets.TryGetFirst(x => x.GUID == guid, out var preset))
        {
            var sm = pc.GetMyStatusManager();
            if(!sm.Ephemeral)
            {
                foreach (var ps in preset.Statuses)
                {
                     var s = sm.Statuses.FirstOrDefault(x => x.GUID == ps);
                     if (!s.Persistent)
                     {
                         sm.Cancel(ps);
                     }
                }
            }
        }
    }
    #endregion MoodlesUpdateManager
}
