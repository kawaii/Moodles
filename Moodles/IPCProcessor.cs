﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Moodles.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkHistory.Delegates;

namespace Moodles;
public class IPCProcessor : IDisposable
{
    [EzIPCEvent] readonly Action Ready;
    [EzIPCEvent] readonly Action Unloading;
    [EzIPCEvent] public readonly Action<IPlayerCharacter> StatusManagerModified;
    [EzIPC("MareSynchronos.GetHandledAddresses", false)] public readonly Func<List<nint>> GetMarePlayers;
    [EzIPC("MareSynchronos.BroadcastMessage", false)] public readonly Action<string> BroadcastMareMessage;

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
    void AcceptMessage(string serMessage)
    {
        if(IncomingMessage.TryDeserialize(Convert.FromBase64String(serMessage), out var message))
        {
            if(message.To == Player.NameWithWorld)
            {
                var sm = Utils.GetMyStatusManager(Player.Object);
                foreach(var x in message.ApplyStatuses)
                {
                    if (Utils.CheckWhitelistGlobal(x) || C.Whitelist.Any(w => w.CheckStatus(x)))
                    {
                        sm.AddOrUpdate(x, false, true);
                    }
                    else
                    {
                        PluginLog.Warning($"Status {x.Title} is not allowed, skipping.");
                    }
                }
            }
        }
    }

    [EzIPC]
    int Version()
    {
        return 1;
    }

    [EzIPC("ClearStatusManagerByName")]
    void ClearStatusManager(string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if (obj == null) return;
        ClearStatusManager((IPlayerCharacter)obj);
    }

    [EzIPC("ClearStatusManagerByPtr")]
    void ClearStatusManager(nint ptr) => ClearStatusManager((IPlayerCharacter)Svc.Objects.CreateObjectReference(ptr));

    [EzIPC("ClearStatusManagerByPC")]
    void ClearStatusManager(IPlayerCharacter pc)
    {
        var m = pc.GetMyStatusManager();
        foreach(var s in m.Statuses)
        {
            if (!s.Persistent)
            {
                m.Cancel(s);
            }
        }
        m.Ephemeral = false;
    }

    [EzIPC("SetStatusManagerByName")]
    void SetStatusManager(string name, string data)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if (obj == null) return;
        SetStatusManager((IPlayerCharacter)obj, data);
    }

    [EzIPC("SetStatusManagerByPtr")]
    void SetStatusManager(nint ptr, string data) => SetStatusManager((IPlayerCharacter)Svc.Objects.CreateObjectReference(ptr), data);

    [EzIPC("SetStatusManagerByPC")]
    void SetStatusManager(IPlayerCharacter pc, string data)
    {
        pc.GetMyStatusManager().Apply(data);
    }

    [EzIPC("GetStatusManagerLP")]
    string GetStatusManager() => GetStatusManager(Player.Object);

    [EzIPC("GetStatusManagerByName")]
    string GetStatusManager(string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is IPlayerCharacter pc && pc.Name.ToString() == name);
        if (obj == null) return null;
        return GetStatusManager((IPlayerCharacter)obj);
    }

    [EzIPC("GetStatusManagerByPtr")]
    string GetStatusManager(nint ptr) => GetStatusManager((IPlayerCharacter)Svc.Objects.CreateObjectReference(ptr));

    [EzIPC("GetStatusManagerByPC")]
    string GetStatusManager(IPlayerCharacter pc)
    {
        if (pc == null) return null;
        return pc.GetMyStatusManager().SerializeToBase64();
    }

    [EzIPC]
    List<MoodlesMoodleInfo> GetRegisteredMoodles()
    {
        var ret = new List<MoodlesMoodleInfo>();
        foreach(var x in C.SavedStatuses)
        {
            P.OtterGuiHandler.MoodleFileSystem.FindLeaf(x, out var path);
            ret.Add((x.GUID, (uint)x.IconID, path?.FullName(), x.Title));
        }
        return ret;
    }

    [EzIPC]
    List<MoodlesProfileInfo> GetRegisteredProfiles()
    {
        var ret = new List<MoodlesProfileInfo>();
        foreach(var x in C.SavedPresets)
        {
            P.OtterGuiHandler.PresetFileSystem.FindLeaf(x, out var path);
            ret.Add((x.GUID, path?.FullName()));
        }
        return ret;
    }

    [EzIPC]
    void AddOrUpdateMoodleByGUID(Guid guid, IPlayerCharacter pc)
    {
        if(C.SavedStatuses.TryGetFirst(x => x.GUID == guid, out var status))
        {
            var sm = pc.GetMyStatusManager();
            if(!sm.Ephemeral)
            {
                sm.AddOrUpdate(status.PrepareToApply(), false, true);
            }
        }
    }

    [EzIPC]
    void AddOrUpdateMoodle((IPlayerCharacter Player, Guid UniqueID, int IconID, string Title, string Description, int StatusType, bool Dispelable, int Stacks, TimeSpan? ExpiresIn) data)
    {
        var status = new MyStatus()
        {
            GUID = data.UniqueID,
            IconID = data.IconID,
            Title = data.Title,
            Description = data.Description,
            Type = (StatusType)data.StatusType,
            Dispelable = data.Dispelable,
            Stacks = data.Stacks,
        };
        if(data.ExpiresIn == null)
        {
            status.NoExpire = true;
        }
        else
        {
            status.Days = data.ExpiresIn.Value.Days;
            status.Minutes = data.ExpiresIn.Value.Minutes;
            status.Seconds = data.ExpiresIn.Value.Seconds;
            status.Hours = data.ExpiresIn.Value.Hours;
        }
        var sm = data.Player.GetMyStatusManager();
        if(!sm.Ephemeral)
        {
            sm.AddOrUpdate(status.PrepareToApply(), false, true);
        }
    }

    [EzIPC]
    void ApplyPresetByGUID(Guid guid, IPlayerCharacter pc)
    {
        if (C.SavedPresets.TryGetFirst(x => x.GUID == guid, out var preset))
        {
            var sm = pc.GetMyStatusManager();
            if (!sm.Ephemeral)
            {
                sm.ApplyPreset(preset);
            }
        }
    }

    [EzIPC]
    void RemoveMoodleByGUID(Guid guid, IPlayerCharacter pc)
    {
        if (C.SavedStatuses.TryGetFirst(x => x.GUID == guid, out var status))
        {
            var sm = pc.GetMyStatusManager();
            if (!sm.Ephemeral)
            {
                foreach(var s in sm.Statuses)
                {
                    if(s.GUID == guid)
                    {
                        s.ExpiresAt = 0;
                    }
                }
            }
        }
    }

    [EzIPC]
    void RemovePresetByGUID(Guid guid, IPlayerCharacter pc)
    {
        if (C.SavedPresets.TryGetFirst(x => x.GUID == guid, out var preset))
        {
            var sm = pc.GetMyStatusManager();
            if (!sm.Ephemeral)
            {
                foreach(var presetStatus in preset.Statuses)
                {
                    foreach (var s in sm.Statuses)
                    {
                        if (s.GUID == presetStatus)
                        {
                            s.ExpiresAt = 0;
                        }
                    }
                }
            }
        }
    }
}
