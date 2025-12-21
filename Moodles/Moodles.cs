using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Moodles.Commands;
using Moodles.Data;
using Moodles.Gui;
using Moodles.OtterGuiHandlers;
using Moodles.Processors;

namespace Moodles;

#pragma warning disable CS8618 // Handled within the constructors TickScheduler(), but for reason intellisense doesnt pick that up.

public class Moodles : IDalamudPlugin
{
    public static Moodles P;
    public Config Config;
    public Memory Memory;
    public CommonProcessor CommonProcessor;
    public CharaWatcher Watcher;
    public static Config C => P.Config;
    public List<MyStatusManager> MyStatusManagers = []; // Currently not used???
    public OtterGuiHandler OtterGuiHandler;
    public Job? LastJob = null;
    private bool LastUIModState = false;
    public StatusSelector StatusSelector;
    public IPCProcessor IPCProcessor;
    public IPCTester IPCTester;

    public Moodles(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this);
        // Define the EzConfig Deserialization factory.
        EzConfig.DefaultSerializationFactory = new MoodleSerializationFactory();
        MoodleSerializationFactory.BackupOldConfigs();

        new TickScheduler(() =>
        {
            Config = EzConfig.Init<Config>();
            EzConfigGui.Init(UI.Draw);
            EzCmd.Add("/moodles", ToggleUi, "Open plugin interface");
            EzCmd.Add("/moodle", MoodleCommandProcessor.Process, "Add or remove moodles");
            // Init the Watcher first, which is only dependent on the Config before it.
            Watcher = new();
            // Init other systems.
            Memory = new();
            CommonProcessor = new();
            OtterGuiHandler = new();
            ProperOnLogin.RegisterAvailable(OnLogin, true);
            new EzFrameworkUpdate(Tick);
            new EzLogout(Logout);
            StatusSelector = new();
            EzConfigGui.Window.SetMinSize(800, 500);
            EzConfigGui.Open();
            CleanupStatusManagers();
            new EzTerritoryChanged((x) => CleanupStatusManagers());
            IPCProcessor = new();
            IPCTester = new();
            Utils.CleanupNulls();
            // Check connected IPC states availability & data.
            IPC.FetchInitial();
        });
    }

    private void ToggleUi(string _, string __)
    {
        if (EzConfigGui.Window is { } window)
        {
            window.IsOpen = !EzConfigGui.Window.IsOpen;
        }
    }

    public void CleanupStatusManagers()
    {
        PluginLog.Debug($"Begin status manager cleanup.");
        foreach(var x in C.StatusManagers.Keys.ToArray())
        {
            var m = C.StatusManagers[x];
            if(m.Statuses.Count == 0 && !m.OwnerValid)
            {
                PluginLog.Debug($"  Deleting empty status manager for {x}");
                C.StatusManagers.Remove(x);
            }
        }
    }

    public bool CanModifyUI()
    {
        if(!C.Enabled) return false;
        if(!C.EnabledDuty)
        {
            if(Svc.Condition[ConditionFlag.BoundByDuty]
                || Svc.Condition[ConditionFlag.BoundByDuty56]
                || Svc.ClientState.IsPvP
                )
            {
                return false;
            }
        }
        if(!C.EnabledCombat)
        {
            if(Svc.Condition[ConditionFlag.InCombat])
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

    // Still need to iterate this every tick, because if new handled players appear,
    // they should be marked as Ephemeral once they appear, and remove Ephemeral once they disappear.
    private unsafe void Tick()
    {
        if(LocalPlayer.Available)
        {
            if((Job)LocalPlayer.ClassJob.RowId != LastJob)
            {
                //JobChange
                LastJob = (Job)LocalPlayer.ClassJob.RowId;
                ApplyAutomation();
            }

            // Need this Tick() check because someone could become a Sundouleia user after being rendered.
            foreach (Character* chara in CharaWatcher.Rendered)
            {
                if (chara->MyStatusManager() is { } sm)
                {
                    if (IPC.SundouleiaPlayerCache.Keys.Contains((nint)chara))
                    {
                        if(!sm.Ephemeral)
                        {
                            PluginLog.Debug($"{chara->GetNameWithWorld()} is now Sundouleia player. Status manager ephemeral, automation disabled.");
                            sm.Ephemeral = true;
                            sm.Statuses.Each(s => s.ExpiresAt = 0);
                        }
                    }
                    else
                    {
                        if (sm.Ephemeral)
                        {
                            PluginLog.Debug($"{chara->GetNameWithWorld()} Sundouleia player removed from rendering. Cleaning up ephemeral status manager.");
                            // Mark them as no longer Ephemeral.
                            sm.Ephemeral = false;
                        }
                    }
                }
            }
        }
        if(CanModifyUI())
        {
            if(!LastUIModState)
            {
                LastUIModState = true;
                InternalLog.Debug($"Can modify UI event");
            }
        }
        else
        {
            if(LastUIModState)
            {
                LastUIModState = false;
                InternalLog.Debug($"Can no longer modify UI");
                CommonProcessor.HideAll();
            }
        }

        if(C.AutoOther) TickOtherPlayerAutomation();
        
        //var toRem = new List<string>();
        // for each(var m in C.StatusManagers)
        //{

        //    if(m.Value.Ephemeral)
        //    {
        //        if(!Svc.Objects.Any(x => x is IPlayerCharacter pc && pc.GetNameWithWorld() == m.Key))
        //        {
        //            toRem.Add(m.Key);
        //        }
        //    }
        //}
        // for each(var m in toRem)
        //{
        //    PluginLog.Debug($"Removing ephemeral status manager for {m}");
        //    C.StatusManagers.Remove(m);
        //    SeenPlayers.RemoveAll(x => x.Name == m);
        //}
    }

    private void OnLogin()
    {
        LastJob = Player.Job;
        C.SeenCharacters.Add(LocalPlayer.NameWithWorld);
        ApplyAutomation();
    }

    public List<(string Name, Job Job)> SeenPlayers = [];
    public unsafe void TickOtherPlayerAutomation()
    {
        List<(string Name, Job Job)> newSeenPlayers = [];
        // Only iterate rendered characters.
        foreach (Character* chara in CharaWatcher.Rendered)
        {
            if ((nint)chara == LocalPlayer.Address) continue;

            var nameWorld = chara->GetNameWithWorld();
            var identifier = (nameWorld, (Job)chara->ClassJob);
            
            // Do logic on unseen players only.
            if (SeenPlayers.Contains(identifier)) continue;

            // Perform Automation logic.
            PluginLog.Debug($"Begin apply automation for {identifier}");
            var mySM = chara->MyStatusManager();

            // Skip if Ephemeral or Sundouleia controlled.
            if (mySM.Ephemeral || IPC.SundouleiaPlayerCache.Keys.Contains((nint)chara))
            {
                PluginLog.Debug($"Skipping automation for {identifier} because status manager is ephemeral or controlled by Sundouleia");
            }
            else
            {
                foreach(var x in chara->GetSuitableAutomation())
                {
                    if(C.SavedPresets.TryGetFirst(a => a.GUID == x.Preset, out var p))
                    {
                        PluginLog.Debug($"Applied preset {p.ID} / {p.Statuses.Select(z => C.SavedStatuses.FirstOrDefault(s => s.GUID == z)?.Title)}");
                        mySM.ApplyPreset(p);
                    }
                }
            }
            newSeenPlayers.Add(identifier);
        }
        SeenPlayers = newSeenPlayers;
    }
    public unsafe void ApplyAutomation(bool forceOtherPlayers = false)
    {
        var clientSM = LocalPlayer.Character->MyStatusManager();
        foreach(var x in LocalPlayer.Character->GetSuitableAutomation())
        {
            if(C.SavedPresets.TryGetFirst(a => a.GUID == x.Preset, out var p))
            {
                clientSM.ApplyPreset(p);
            }
        }
        if(forceOtherPlayers) SeenPlayers.Clear();
    }

    public void Dispose()
    {
        Safe(() => CleanupStatusManagers());
        Safe(() => IPCProcessor?.Dispose());
        Safe(() => CommonProcessor?.Dispose());
        Safe(() => Memory?.Dispose());
        Safe(() => Watcher?.Dispose());
        ECommonsMain.Dispose();
        P = null!;
    }
}

#pragma warning restore CS8618 // Handled within the constructors TickScheduler(), but for reason intellisense doesnt pick that up.
