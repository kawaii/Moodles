using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using Moodles.Commands;
using Moodles.Data;
using Moodles.Gui;
using Moodles.OtterGuiHandlers;
using Moodles.Processors;

namespace Moodles;

public class Moodles : IDalamudPlugin
{
    public static Moodles P;
    public Config Config;
    public Memory Memory;
    public CommonProcessor CommonProcessor;
    public static Config C => P.Config;
    public List<MyStatusManager> MyStatusManagers = [];
    public OtterGuiHandler OtterGuiHandler;
    public Job? LastJob = null;
    bool LastUIModState = false;
    public StatusSelector StatusSelector;
    public IPCProcessor IPCProcessor;
    public IPCTester IPCTester;

    public Moodles(DalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this);
        new TickScheduler(() =>
        {
            Config = EzConfig.Init<Config>();
            EzConfigGui.Init(UI.Draw);
            EzCmd.Add("/moodles", EzConfigGui.Open, "Open plugin interface");
            EzCmd.Add("/moodle", MoodleCommandProcessor.Process, "Add or remove moodles");
            Memory = new();
            CommonProcessor = new();
            OtterGuiHandler = new();
            ProperOnLogin.RegisterAvailable(OnLogin, true);
            new EzFrameworkUpdate(Tick);
            new EzLogout(Logout);
            StatusSelector = new();
            EzConfigGui.Window.SetMinSize(800, 500);
            CleanupStatusManagers();
            new EzTerritoryChanged((x) => CleanupStatusManagers());
            IPCProcessor = new();
            IPCTester = new();
            Utils.CleanupNulls();
        });
    }

    public void CleanupStatusManagers()
    {
        PluginLog.Debug($"Begin status manager cleanup.");
        foreach(var x in C.StatusManagers.Keys.ToArray())
        {
            var m = C.StatusManagers[x];
            if(m.Statuses.Count == 0)
            {
                PluginLog.Debug($"  Deleting empty status manager for {x}");
                C.StatusManagers.Remove(x);
            }
        }
    }

    public bool CanModifyUI()
    {
        if(!C.Enabled) return false;
        if (!C.EnabledDuty)
        {
            if (Svc.Condition[ConditionFlag.BoundByDuty]
                || Svc.Condition[ConditionFlag.BoundByDuty56]
                || Svc.ClientState.IsPvP
                )
            {
                return false;
            }
        }
        if (!C.EnabledCombat)
        {
            if (Svc.Condition[ConditionFlag.InCombat])
            {
                return false;
            }
        }
        return true;
    }

    private void Logout()
    {
        LastJob = null;
    }

    private void Tick()
    {
        if (Player.Available)
        {
            if(Player.Job != LastJob)
            {
                LastJob = Player.Job;
                //JobChange
                ApplyAutomation();
            }
            var marePlayers = Utils.GetMarePlayers();
            foreach(var x in Svc.Objects)
            {
                if(x is PlayerCharacter pc)
                {
                    var m = pc.GetMyStatusManager(false);
                    if(m != null)
                    {
                        if (marePlayers.Contains(pc.Address))
                        {
                            if (!m.Ephemeral)
                            {
                                PluginLog.Debug($"{pc.GetNameWithWorld()} is now Mare player. Status manager ephemeral, automation disabled.");
                                m.Ephemeral = true;
                                m.Statuses.Each(s => s.ExpiresAt = 0);
                            }
                        }
                        else
                        {
                            if(m.Ephemeral)
                            {
                                PluginLog.Debug($"{pc.GetNameWithWorld()} is no longer Mare player. Status manager persistent, automation enabled.");
                                m.Ephemeral = false;
                            }
                        }
                    }
                }
            }
        }
        if (CanModifyUI())
        {
            if (!LastUIModState)
            {
                LastUIModState = true;
                InternalLog.Debug($"Can modify UI event");
            }
        }
        else
        {
            if (LastUIModState)
            {
                LastUIModState = false;
                InternalLog.Debug($"Can no longer modify UI");
                this.CommonProcessor.HideAll();
            }
        }
        if(C.AutoOther) TickOtherPlayerAutomation();
        var toRem = new List<string>();
        foreach(var m in C.StatusManagers)
        {
            if (m.Value.Ephemeral)
            {
                if(!Svc.Objects.Any(x => x is PlayerCharacter pc && pc.GetNameWithWorld() == m.Key))
                {
                    toRem.Add(m.Key);
                }
            }
        }
        foreach(var m in toRem)
        {
            PluginLog.Debug($"Removing ephemeral status manager for {m}");
            C.StatusManagers.Remove(m);
            SeenPlayers.RemoveAll(x => x.Name == m);
        }
    }

    private void OnLogin()
    {
        LastJob = Player.Job;
        C.SeenCharacters.Add(Player.NameWithWorld);
        ApplyAutomation();
    }

    public List<(string Name, Job Job)> SeenPlayers = [];
    public void TickOtherPlayerAutomation()
    {
        List<(string Name, Job Job)> newSeenPlayers = [];
        foreach (var q in Svc.Objects)
        {
            if (q?.Address != Player.Object?.Address && q is PlayerCharacter pc)
            {
                var name = pc.GetNameWithWorld();
                var identifier = (name, pc.GetJob());
                if (!SeenPlayers.Contains(identifier))
                {
                    PluginLog.Debug($"Begin apply automation for {identifier}");
                    var mgr = Utils.GetMyStatusManager(name);
                    if (mgr.Ephemeral || Utils.GetMarePlayers().Contains(pc.Address))
                    {
                        PluginLog.Debug($"Skipping automation for {identifier} because status manager is controlled by an external plugin");
                    }
                    else
                    {
                        foreach (var x in Utils.GetSuitableAutomation(pc))
                        {
                            if (C.SavedPresets.TryGetFirst(a => a.GUID == x.Preset, out var p))
                            {
                                PluginLog.Debug($"  Applied preset {p.ID} / {p.Statuses.Select(z => C.SavedStatuses.FirstOrDefault(s => s.GUID == z)?.Title)}");
                                mgr.ApplyPreset(p);
                            }
                        }
                    }
                }
                newSeenPlayers.Add(identifier);
            }
        }
        SeenPlayers = newSeenPlayers;
    }
    public void ApplyAutomation(bool forceOtherPlayers = false)
    {
        {
            var mgr = Utils.GetMyStatusManager(Player.Object);
            foreach (var x in Utils.GetSuitableAutomation())
            {
                if (C.SavedPresets.TryGetFirst(a => a.GUID == x.Preset, out var p))
                {
                    mgr.ApplyPreset(p);
                }
            }
        }
        if (forceOtherPlayers) this.SeenPlayers.Clear();
    }

    public void Dispose()
    {
        Safe(() => CleanupStatusManagers());
        Safe(() => IPCProcessor?.Dispose());
        Safe(() => CommonProcessor?.Dispose());
        Safe(() => Memory?.Dispose());
        ECommonsMain.Dispose();
        P = null;
    }
}