using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.EzIpcManager;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Moodles.Data;
using Moodles.Gui;

namespace Moodles;
#pragma warning disable CS0649, CS8602, CS8618 // EzIPC never needs to be initialized, it is handled internally.

public class IPCProcessor : IDisposable
{
    [EzIPCEvent] private readonly Action Ready;
    [EzIPCEvent] private readonly Action Unloading;

    /// <summary>
    ///     Triggered when a monitored player's Status Manager is changes. <para />
    ///     Address passed is a valid Character* / IPlayerCharacter.
    /// </summary>
    [EzIPCEvent] public readonly Action<nint> StatusManagerModified;

    /// <summary>
    ///     Triggered when a <see cref="MyStatus"/> is updated, added, or removed. (2nd parameter indicates removal)
    /// </summary>
    [EzIPCEvent] public readonly Action<Guid, bool> StatusUpdated;

    /// <summary>
    ///     Triggered when a <see cref="Preset"/> is updated, added, or removed. (2nd parameter indicates removal)
    /// </summary>
    [EzIPCEvent] public readonly Action<Guid, bool> PresetUpdated;

    // TODO ADD SOEMTHING LIKE THIS
    //[EzIPCEvent] public readonly Action<nint, List<MoodlesStatusInfo>, bool> OnApplyToTarget;

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
        return 4;
    }

    #region StatusManager
    [EzIPC("ClearStatusManagerByNameV2")]
    private unsafe void ClearStatusManager(string name)
    {
        if (CharaWatcher.TryGetFirst(x => x.GetNameWithWorld() == name || x.NameString == name, out var chara))
        {
            ClearStatusManagerInternal(chara);
        }
    }

    [EzIPC("ClearStatusManagerByPtrV2")]
    private void ClearStatusManager(nint ptr)
    {
        if (!CharaWatcher.Rendered.Contains(ptr)) return;
        ClearStatusManagerInternal(ptr);
    }

    [EzIPC("ClearStatusManagerByPlayerV2")]
    private void ClearStatusManager(IPlayerCharacter pc) => ClearStatusManagerInternal(pc.Address);

    /// <summary> 
    ///     Clears the encoded base64 data to a visible player's StatusManager, if rendered.
    /// </summary>
    private unsafe void ClearStatusManagerInternal(nint charaAddr)
    {
        Character* chara = (Character*)charaAddr;
        if (chara == null)
        {
            PluginLog.LogWarning("[IPC] Clear Status Manager Chara is NULL");
            return;
        }

        var mySM = chara->MyStatusManager();
        foreach (var s in mySM.Statuses)
        {
            if (!s.Persistent)
            {
                mySM.Cancel(s);
            }
        }
        mySM.Ephemeral = false;
    }

    [EzIPC("SetStatusManagerByNameV2")]
    private void SetStatusManager(string name, string data)
    {
        if (CharaWatcher.TryGetFirst(x => x.GetNameWithWorld() == name || x.NameString == name, out var chara))
        {
            SetStatusManagerInternal(chara, data);
        }
    }

    [EzIPC("SetStatusManagerByPtrV2")]
    private void SetStatusManager(nint ptr, string data)
    {
        if (!CharaWatcher.Rendered.Contains(ptr)) return;
        SetStatusManagerInternal(ptr, data);
    }

    [EzIPC("SetStatusManagerByPlayerV2")]
    private void SetStatusManager(IPlayerCharacter pc, string data) => SetStatusManagerInternal(pc.Address, data);

    /// <summary> 
    ///     Applies the encoded base64 data to a visible player's StatusManager, if rendered.
    /// </summary>
    private unsafe void SetStatusManagerInternal(nint charaAddr, string data)
    {
        Character* chara = (Character*)charaAddr;
        if (chara == null)
        {
            PluginLog.LogWarning("[IPC] Set Status Manager Chara is NULL");
            return;
        }
        chara->MyStatusManager().Apply(data);
    }

    /// <summary>
    ///     Returns the StatusManager for the Client Player.
    /// </summary>
    [EzIPC("GetClientStatusManagerV2")]
    private string GetStatusManager() => GetStatusManagerInternal(LocalPlayer.Address);

    [EzIPC("GetStatusManagerByNameV2")]
    private string GetStatusManager(string name) => CharaWatcher.TryGetFirst(x => x.GetNameWithWorld() == name || x.NameString == name, out var chara)
        ? GetStatusManagerInternal(chara) : null!;

    [EzIPC("GetStatusManagerByPtrV2")]
    private string GetStatusManager(nint ptr) => CharaWatcher.Rendered.Contains(ptr)
        ? GetStatusManagerInternal(ptr) : null!;

    [EzIPC("GetStatusManagerByPlayerV2")]
    private string GetStatusManager(IPlayerCharacter pc) => GetStatusManagerInternal(pc.Address);

    /// <summary>
    ///     Returns the base64 encoded StatusManager data for the player, if rendered.
    /// </summary>
    private unsafe string GetStatusManagerInternal(nint charaAddr)
    {
        Character* chara = (Character*)charaAddr;
        if (chara == null)
        {
            PluginLog.LogWarning("[IPC] Get Status Manager Chara is NULL");
            return null!;
        }
        return chara->MyStatusManager().SerializeToBase64();
    }

    /// <summary>
    ///     Fetches the Info of the Statuses active on the Client StatusManager.
    /// </summary>
    [EzIPC("GetClientStatusManagerInfoV2")]
    private List<MoodlesStatusInfo> GetClientStatusManagerInfo() => GetStatusManagerInfoInternal(LocalPlayer.Address);

    [EzIPC("GetStatusManagerInfoByNameV2")]
    private List<MoodlesStatusInfo> GetStatusManagerInfo(string name)
    {
        return CharaWatcher.TryGetFirst(x => x.GetNameWithWorld() == name || x.NameString == name, out var chara)
            ? GetStatusManagerInfoInternal(chara) : new List<MoodlesStatusInfo>();
    }

    [EzIPC("GetStatusManagerInfoByPtrV2")]
    private List<MoodlesStatusInfo> GetStatusManagerInfo(nint ptr) => GetStatusManagerInfoInternal(ptr);

    [EzIPC("GetStatusManagerInfoByPlayerV2")]
    private List<MoodlesStatusInfo> GetStatusManagerInfo(IPlayerCharacter pc) => GetStatusManagerInfoInternal(pc.Address);

    /// <summary>
    ///     Gets the MyStatus info in tuple format for the player, if rendered.
    /// </summary>
    private unsafe List<MoodlesStatusInfo> GetStatusManagerInfoInternal(nint charaAddr)
    {
        Character* chara = (Character*)charaAddr;
        if (chara == null)
        {
            PluginLog.LogWarning("[IPC] Get Status Manager Info Chara is NULL");
            return new List<MoodlesStatusInfo>();
        }
        return chara->MyStatusManager().GetActiveStatusInfo();
    }
    #endregion StatusManager

    #region MoodlesInfoFetch
    /// <summary>
    ///     Obtain the MyStatus tuple for a valid Status GUID.
    /// </summary>
    [EzIPC]
    private MoodlesStatusInfo GetStatusInfoV2(Guid guid)
        => C.SavedStatuses.TryGetFirst(x => x.GUID == guid, out var status)
            ? status.ToStatusTuple() : default;

    /// <summary>
    ///     Obtain all MyStatus tuples from the Clients statuses.
    /// </summary>
    [EzIPC]
    private List<MoodlesStatusInfo> GetStatusInfoListV2() => C.SavedStatuses.Select(x => x.ToStatusTuple()).ToList();

    /// <summary>
    ///     Obtain the Preset tuple for a valid Preset GUID.
    /// </summary>
    [EzIPC]
    private MoodlePresetInfo GetPresetInfoV2(Guid guid)
        => C.SavedPresets.TryGetFirst(x => x.GUID == guid, out var preset)
            ? preset.ToPresetInfoTuple() : new(Guid.Empty, [], PresetApplicationType.UpdateExisting, string.Empty);

    /// <summary>
    ///     Obtain all Preset tuples from the Clients presets.
    /// </summary>
    [EzIPC]
    private List<MoodlePresetInfo> GetPresetsInfoListV2() => C.SavedPresets.Select(x => x.ToPresetInfoTuple()).ToList();

    /// <summary>
    ///     Shorter format of fetched MyStatus Info, holding GUID, IconID, Path, and Title.
    /// </summary>
    [EzIPC]
    private List<MoodlesMoodleInfo> GetRegisteredMoodlesV2()
    {
        var ret = new List<MoodlesMoodleInfo>();
        foreach (var x in C.SavedStatuses)
        {
            if (P.OtterGuiHandler.MoodleFileSystem.FindLeaf(x, out var path))
                ret.Add((x.GUID, (uint)x.IconID, path.FullName(), x.Title));
        }
        return ret;
    }

    /// <summary>
    ///     Shorter format of fetched Preset Info, holding GUID, and FullPath.
    /// </summary>
    [EzIPC]
    private List<MoodlesProfileInfo> GetRegisteredProfilesV2()
    {
        var ret = new List<MoodlesProfileInfo>();
        foreach (var x in C.SavedPresets)
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

    [EzIPC]
    private void AddOrUpdateStatusByNameV2(Guid guid, string name)
    {
        if (CharaWatcher.TryGetFirst(x => x.GetNameWithWorld() == name || x.NameString == name, out var chara))
        {
            AddOrUpdateMoodleInternal(chara, guid);
        }
    }

    [EzIPC]
    private void AddOrUpdateMoodleByPtrV2(Guid guid, nint ptr)
    {
        if (!CharaWatcher.Rendered.Contains(ptr)) return;
        AddOrUpdateMoodleInternal(ptr, guid);
    }

    [EzIPC]
    private void AddOrUpdateMoodleByPlayerV2(Guid guid, IPlayerCharacter pc) => AddOrUpdateMoodleInternal(pc.Address, guid);

    /// <summary>
    ///     Adds a Status by GUID to the valid player, or reapplies it if already present.
    /// </summary>
    private unsafe void AddOrUpdateMoodleInternal(nint charaAddr, Guid guid)
    {
        Character* chara = (Character*)charaAddr;
        if (chara == null)
        {
            PluginLog.LogWarning("[IPC] AddOrUpdate Moodle Chara is NULL");
            return;
        }
        if (C.SavedStatuses.TryGetFirst(x => x.GUID == guid, out var status))
        {
            var sm = chara->MyStatusManager();
            if (!sm.Ephemeral)
            {
                PluginLog.LogDebug($"Adding or Updating Moodle {status.Title} to {chara->GetNameWithWorld()}");
                sm.AddOrUpdate(status.PrepareToApply(), UpdateSource.StatusTuple, false, true);
            }
        }
    }

    [EzIPC]
    private void ApplyPresetByNameV2(Guid guid, string name)
    {
        if (CharaWatcher.TryGetFirst(x => x.GetNameWithWorld() == name || x.NameString == name, out var chara))
        {
            ApplyPresetInternal(chara, guid);
        }
    }

    [EzIPC]
    private void ApplyPresetByPtrV2(Guid guid, nint ptr)
    {
        if (!CharaWatcher.Rendered.Contains(ptr)) return;
        ApplyPresetInternal(ptr, guid);
    }
    [EzIPC]
    private void ApplyPresetByPlayerV2(Guid guid, IPlayerCharacter pc) => ApplyPresetInternal(pc.Address, guid);

    /// <summary>
    ///     Applies a Preset to the valid player from the Presets by <paramref name="guid"/>
    /// </summary>
    private unsafe void ApplyPresetInternal(nint charaAddr, Guid guid)
    {
        Character* chara = (Character*)charaAddr;
        if (chara == null)
        {
            PluginLog.LogWarning("[IPC] Apply Preset Chara is NULL");
            return;
        }
        if (C.SavedPresets.TryGetFirst(x => x.GUID == guid, out var preset))
        {
            var sm = chara->MyStatusManager();
            if (!sm.Ephemeral)
            {
                sm.ApplyPreset(preset);
            }
        }
    }

    [EzIPC]
    private void RemoveMoodleByPlayerV2(Guid guid, IPlayerCharacter pc) => RemoveMoodleInternal(pc.Address, guid);

    /// <summary>
    ///     Remove a single Status from the valid players StatusManager.
    /// </summary>
    private unsafe void RemoveMoodleInternal(nint charaAddr, Guid guid)
    {
        Character* chara = (Character*)charaAddr;
        var sm = chara->MyStatusManager();

        if (sm.Statuses.TryGetFirst(x => x.GUID == guid, out var status))
        {
            if (!sm.Ephemeral)
            {
                if (!status.Persistent)
                {
                    sm.Cancel(guid);
                }
            }
        }
    }


    [EzIPC]
    private void RemoveMoodlesByNameV2(List<Guid> guids, string name)
    {
        if (CharaWatcher.TryGetFirst(x => x.GetNameWithWorld() == name || x.NameString == name, out var chara))
        {
            RemoveMoodlesInternal(chara, guids);
            return;
        }
    }

    [EzIPC]
    private void RemoveMoodlesByPtrV2(List<Guid> guids, nint ptr)
    { 
        if (!CharaWatcher.Rendered.Contains(ptr)) return;
        RemoveMoodlesInternal(ptr, guids);
    }

    [EzIPC]
    private void RemoveMoodlesByPlayerV2(List<Guid> guids, IPlayerCharacter pc) => RemoveMoodlesInternal(pc.Address, guids);

    /// <summary>
    ///     Removes any Statuses from the valid player's StatusManager identified in <paramref name="guids"/>.
    /// </summary>
    private unsafe void RemoveMoodlesInternal(nint charaAddr, List<Guid> guids)
    {
        Character* chara = (Character*)charaAddr;
        if (chara == null)
        {
            PluginLog.LogWarning("[IPC] Remove Moodles Chara is NULL");
            return;
        }
        var sm = chara->MyStatusManager();
        foreach (var guid in guids)
        {
            if (sm.Statuses.TryGetFirst(x => x.GUID == guid, out var status))
            {
                if (!sm.Ephemeral)
                {
                    if (!status.Persistent)
                    {
                        sm.Cancel(guid);
                    }
                }
            }
        }
    }

    [EzIPC]
    private void RemovePresetByPlayerV2(Guid guid, IPlayerCharacter pc) => RemovePresetInternal(pc.Address, guid);

    private unsafe void RemovePresetInternal(nint charaAddr, Guid guid)
    {
        Character* chara = (Character*)charaAddr;
        if (chara == null)
        {
            PluginLog.LogWarning("[IPC] Remove Preset Chara is NULL");
            return;
        }
        // preset must exist in our saved presets since presets are not stored in the Status Manager
        if (C.SavedPresets.TryGetFirst(x => x.GUID == guid, out var preset))
        {
            var sm = chara->MyStatusManager();
            if(!sm.Ephemeral)
            {
                foreach (var ps in preset.Statuses)
                {
                     if (sm.Statuses.FirstOrDefault(x => x.GUID == ps) is { } status && !status.Persistent)
                     {
                         sm.Cancel(ps);
                     }
                }
            }
        }
    }

    void IDisposable.Dispose() => throw new NotImplementedException();
    #endregion MoodlesUpdateManager
}
#pragma warning restore CS0649, CS8602, CS8618


