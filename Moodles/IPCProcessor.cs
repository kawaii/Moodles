using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using Moodles.Data;
using System.Collections.Immutable;

namespace Moodles;
public class IPCProcessor : IDisposable
{
    [EzIPCEvent] private readonly Action Ready;
    [EzIPCEvent] private readonly Action Unloading;

    /// <summary>
    ///     Fired whenever the status manager being handled on any monitored player is modified.
    /// </summary>
    [EzIPCEvent] public readonly Action<IPlayerCharacter> StatusManagerModified;

    /// <summary>
    ///     Event that fires whenever the client changes the settings of a moodle in their Moodles list. <para />
    ///     Does not fire upon moodle application or removal. Only refers to client player.
    /// </summary>
    [EzIPCEvent] public readonly Action<Guid> StatusModified;

    /// <summary>
    ///     Event that fires whenever the client updates the list of statuses a specific preset applies. <para /> 
    ///     Does not fire upon preset activation or deactivation. Only refers to client player.
    /// </summary>
    [EzIPCEvent] public readonly Action<Guid> PresetModified;

    /// <summary>
    ///     Retrieves the list of Project GSpeak's visible pairs, with the bi-directional permissions. <para />
    ///     Each pair's string holds the <c>playername@world</c>
    /// </summary>
    [EzIPC("GagSpeak.GetHandledVisiblePairs", false)] public readonly Func<List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)>> GetGSpeakPlayers;

    /// <summary>
    ///     A request sourced from <b>Moodles</b> to apply a Status/Preset to another GSpeak pair. <para />
    ///     This is <b>permission-validated by GSpeak</b> against the client and targets perms, ensuring valid application.
    /// </summary>
    [EzIPC("GagSpeak.ApplyStatusesToPairRequest", false)] public readonly Action<string, string, List<MoodlesStatusInfo>, bool> ApplyStatusesToGSpeakPair;

    /// <summary>
    ///     Event invoked by <b>GSpeak</b> whenever their list of visible pairs changed. <para />
    ///     Helps keep Moodles whitelist cache for GSpeak synced without pulling the list every tick.
    /// </summary>
    [EzIPCEvent("GagSpeak.VisiblePairsUpdated", false)]
    private void VisiblePairsUpdated()
    {
        new TickScheduler(() => Utils.GetGSpeakPlayers());
    }

    /// <summary>
    ///     Fired when GSpeak's IPCProvider has finished initialization.
    /// </summary>
    [EzIPCEvent("GagSpeak.Ready", false)]
    private void GagSpeakReady()
    {
        new TickScheduler(() =>
        {
            PluginLog.LogDebug("GagSpeak Initialized, Fetching Initial List of Pairs.");
            Utils.GSpeakAvailable = true;
            Utils.GetGSpeakPlayers();
        });
    }

    /// <summary>
    ///     Fired whenever GSpeak's IPCProvider has called its disposal method.
    /// </summary>
    [EzIPCEvent("GagSpeak.Disposing", false)]
    private void GagSpeakDisposing()
    {
        new TickScheduler(() =>
        {
            PluginLog.LogDebug("GagSpeak Disposed / Disabled. Clearing List of GSpeak Players.");
            Utils.ClearGSpeakPlayers();
            Utils.GSpeakAvailable = false;
        });
    }

    /// <summary>
    ///     An event invoked by GSpeak to apply a MoodleStatus by its tuple information. <para />
    ///     This does not require any permission checks because it can ONLY be applied to the client, not other players. <para />
    ///     Sourced from GSpeak only, ensuring allowance for the action was validated. <para />
    ///     Primarily used as a 'Try-On' feature to preview Moodles stored in GSpeak's Moodle ShareHub Database.
    /// </summary>
    [EzIPCEvent("GagSpeak.ApplyMoodleStatus", false)]
    private void ApplyMoodleStatus(MoodlesStatusInfo status)
    {
        new TickScheduler(() =>
        {
            if (Player.Object is null) return;
            PluginLog.LogDebug($"GSpeak applied a status to the client: ({status.Title})");
            var sm = Utils.GetMyStatusManager(Player.Object);
            sm.AddOrUpdate(MyStatus.FromStatusInfoTuple(status).PrepareToApply(), UpdateSource.StatusTuple, false, true);
        });
    }

    /// <summary>
    ///     Effectively <c>ApplyMoodleStatus</c>, except with a list of StatusInfo's. <para />
    ///     Primarily used as a 'Try-On' feature to preview Moodle Presets stored in GSpeak's Moodle ShareHub Database.
    /// </summary>
    [EzIPCEvent("GagSpeak.ApplyMoodleStatusList", false)]
    private void ApplyMoodleStatusList(List<MoodlesStatusInfo> statuses)
    {
        new TickScheduler(() =>
        {
            if (Player.Object is null) return;
            PluginLog.LogDebug($"GSpeak applied a list of statuses to the client: ({string.Join(",", statuses.Select(s => s.Title))})");
            var sm = Utils.GetMyStatusManager(Player.Object);
            foreach (var status in statuses)
            {
                sm.AddOrUpdate(MyStatus.FromStatusInfoTuple(status).PrepareToApply(), UpdateSource.StatusTuple, false, true);
            }
        });
    }

    public IPCProcessor()
    {
        EzIPC.Init(this);
        Ready();
    }

    public void Dispose()
    {
        Unloading();
    }

    /// <summary>
    ///     Applies the requested statuses to the client player from the sender.
    /// </summary>
    /// <param name="senderNameWorld"> The name of the sender player. </param>
    /// <param name="intendedRecipient"> The intended recipient (Should always match client player) </param>
    /// <param name="statusesToApply"> The list of statuses to apply to the client player. </param>
    /// <returns> True if the client is a mare user. False if they are not. (Us, not the sender) </returns>
    [EzIPC("ApplyStatusesFromGSpeakPair")]
    private void ApplyStatusesFromGSpeakPair(string senderNameWorld, string intendedRecipient, List<MoodlesStatusInfo> statusesToApply)
    {
        if(!(intendedRecipient == Player.NameWithWorld))
        {
            PluginLog.Warning("An update to your status was recieved, but the intended recipient was not you.");
            return;
        }
        else
        {
            // see if the sender is in our list of GSpeak players.
            var gSpeakPlayer = Utils.GSpeakPlayers.FirstOrDefault(w => w.Item1 == senderNameWorld);
            if(gSpeakPlayer != default)
            {
                // Fetch the status manager of our player object.
                var sm = Utils.GetMyStatusManager(Player.Object);
                var perms = gSpeakPlayer.Item2; // client perms for pair.
                foreach(var x in statusesToApply)
                {
                    if(C.WhitelistGSpeak.Any(w => w.CheckStatus(perms, x.NoExpire)))
                    {
                        sm.AddOrUpdate(MyStatus.FromStatusInfoTuple(x).PrepareToApply(), UpdateSource.StatusTuple, false, true);
                    }
                }
            }
        }
    }

    /// <summary> 
    /// Returns the version of Moodle's IPC. 
    /// </summary>
    [EzIPC]
    private int Version()
    {
        return 1;
    }

    #region StatusManager
    /// <summary> 
    ///     Attempts to clear the active Moodles on a player using their name. <para />
    ///     Does not complete if a player by this name is not found within the object table.
    /// </summary>
    [EzIPC("ClearStatusManagerByName")]
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
    /// <param name="ptr"> The object address to search for in the object table. </param>
    [EzIPC("ClearStatusManagerByPtr")]
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
    /// <param name="pc"> The PlayerCharacter object to clear the status from. </param>
    [EzIPC("ClearStatusManagerByPC")]
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
    /// <param name="name"> The object name to search for in the object table. </param>
    /// <param name="data"> The base64 encoded status manager data to apply to the player. </param>
    [EzIPC("SetStatusManagerByName")]
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
    /// <param name="ptr"> The object address to search for in the object table. </param>
    /// <param name="data"> The base64 encoded status manager data to apply to the player. </param>
    [EzIPC("SetStatusManagerByPtr")]
    private void SetStatusManager(nint ptr, string data) => SetStatusManager((IPlayerCharacter)Svc.Objects.CreateObjectReference(ptr), data);


    /// <summary> 
    ///     Attempts to apply the encoded base64 status manager data to a visible player character object by the object reference. <para />
    ///     If the object is null or not present, this method will do nothing.
    /// </summary>
    /// <param name="pc"> The PlayerCharacter object to apply the status manager data to. </param>
    /// <param name="data"> The base64 encoded status manager data to apply to the player. </param>
    [EzIPC("SetStatusManagerByPC")]
    private void SetStatusManager(IPlayerCharacter pc, string data)
    {
        pc.GetMyStatusManager().Apply(data);
    }


    /// <summary>
    ///     Fetches the current status manager data of the client's player character.
    /// </summary>
    /// <returns> The base64 encoded status manager data of the player character. </returns>
    [EzIPC("GetStatusManagerLP")]
    private string GetStatusManager() => GetStatusManager(Player.Object);


    /// <summary>
    ///     Fetches the current status manager data of a player character by their name.
    /// </summary>
    /// <returns> The base64 encoded status manager data of the player character. </returns>
    /// <param name="name"> The object name to search for in the object table. </param>
    [EzIPC("GetStatusManagerByName")]
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
    /// <param name="ptr"> The object address to search for in the object table. </param>
    [EzIPC("GetStatusManagerByPtr")]
    private string GetStatusManager(nint ptr) => GetStatusManager((IPlayerCharacter)Svc.Objects.CreateObjectReference(ptr));


    /// <summary>
    ///     Fetches the current status manager data of a player character by the IPlayerCharacter object reference.
    /// </summary>
    /// <returns> The base64 encoded status manager data of the player character. </returns>
    /// <param name="pc"> The PlayerCharacter object to fetch the status manager data from. </param>
    [EzIPC("GetStatusManagerByPC")]
    private string GetStatusManager(IPlayerCharacter pc)
    {
        if(pc == null) return null;
        return pc.GetMyStatusManager().SerializeToBase64();
    }

    /// <summary>
    ///     Fetches the Info of the Statuses active on the Client Player's StatusManager.
    /// </summary>
    /// <returns> The status info tuple of the Statuses Active in the StatusManager. </returns>
    [EzIPC("GetStatusManagerInfoLP")]
    private List<MoodlesStatusInfo> GetStatusManagerInfo() => GetStatusManagerInfo(Player.Object);

    /// <summary>
    ///     Fetches the Info of the Statuses active on the StatusManager associated with the name.
    /// </summary>
    /// <param name="name"> PlayerName used to locate which StatusManager we retrieve the tuple list from. </param>
    /// <returns> The status info tuple of the Statuses Active in the StatusManager. </returns>
    [EzIPC("GetStatusManagerInfoByName")]
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
    /// <param name="ptr"> Address used to locate which StatusManager we retrieve the tuple list from. </param>
    [EzIPC("GetStatusManagerInfoByPtr")]
    private List<MoodlesStatusInfo> GetStatusManagerInfo(nint ptr) => GetStatusManagerInfo((IPlayerCharacter)Svc.Objects.CreateObjectReference(ptr));


    /// <summary>
    ///     Fetches the current status manager data of a player character by the IPlayerCharacter object reference.
    /// </summary>
    /// <returns> The base64 encoded status manager data of the player character. </returns>
    /// <param name="pc"> The PlayerCharacter object to fetch the status manager data from. </param>
    [EzIPC("GetStatusManagerInfoByPC")]
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
    /// <param name="guid"> The Identifier for the existing Moodle. </param>
    [EzIPC("GetRegisteredMoodleInfo")]
    private MoodlesStatusInfo GetRegisteredMoodleInfo(Guid guid)
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
    [EzIPC("GetRegisteredMoodlesInfo")]
    private List<MoodlesStatusInfo> GetRegisteredMoodlesInfo()
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
    /// <param name="guid"> The Identifier for the existing Profile. </param>
    [EzIPC("GetRegisteredPresetInfo")]
    private MoodlePresetInfo GetRegisteredPresetInfo(Guid guid)
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
    [EzIPC("GetRegisteredPresetsInfo")]
    private List<MoodlePresetInfo> GetRegisteredPresetsInfo()
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
    private List<MoodlesMoodleInfo> GetRegisteredMoodles()
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
    private List<MoodlesProfileInfo> GetRegisteredProfiles()
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
    [EzIPC("AddOrUpdateMoodleByGUIDByName")]
    private void AddOrUpdateMoodleByGUID(Guid guid, string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return;
        AddOrUpdateMoodleByGUID(guid, (IPlayerCharacter)obj);
    }

    /// <summary>
    ///     Appends a Preset from the client's Preset List to the status manager of a visible player.
    /// </summary>
    [EzIPC]
    private void AddOrUpdateMoodleByGUID(Guid guid, IPlayerCharacter pc)
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
    [EzIPC("ApplyPresetByGUIDByName")]
    private void ApplyPresetByGUID(Guid guid, string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return;
        ApplyPresetByGUID(guid, (IPlayerCharacter)obj);
    }

    /// <summary>
    ///     Appends a Preset from the client's Preset List to the status manager of a visible player.
    /// </summary>
    [EzIPC]
    private void ApplyPresetByGUID(Guid guid, IPlayerCharacter pc)
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
    private void RemoveMoodleByGUID(Guid guid, IPlayerCharacter pc)
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
    [EzIPC("RemoveMoodlesByGUIDByName")]
    private void RemoveMoodlesByGUID(List<Guid> guids, string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if(obj == null) return;
        RemoveMoodlesByGUID(guids, (IPlayerCharacter)obj);
    }

    /// <summary>
    ///     Removes a list of Statuses from the a visible players active status manager.
    /// </summary>
    [EzIPC]
    private void RemoveMoodlesByGUID(List<Guid> guids, IPlayerCharacter pc)
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
    private void RemovePresetByGUID(Guid guid, IPlayerCharacter pc)
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
