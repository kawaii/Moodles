using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
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

    public List<(string Name, Job Job)> SeenPlayers = [];
    
    public Moodles(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this);
        // Define the EzConfig Deserialization factory.
        EzConfig.DefaultSerializationFactory = new MoodleSerializationFactory();
        MoodleSerializationFactory.BackupOldConfigs();
      
        PluginLog.Warning("Init");

        new TickScheduler(() =>
        {
            PluginLog.Warning("TickScheduler");

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
            CleanupStatusManagers();
            PurgeEphemeralManagers();
            new EzTerritoryChanged((x) => CleanupStatusManagers());
            IPCProcessor = new();
            IPCTester = new();
            Utils.CleanupNulls();
            PluginLog.Warning("TickScheduler END");
            // Check connected IPC states availability & data.
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

    public void PurgeEphemeralManagers()
    {
        foreach(var x in C.StatusManagers.Keys.ToArray())
        {
            if(C.StatusManagers[x].Ephemeral)
            {
                PluginLog.Debug($"  Purging ephemeral status manager for {x}");
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
            }

    private void OnLogin()
    {
        LastJob = (Job)LocalPlayer.ClassJob.RowId;
        C.SeenCharacters.Add(LocalPlayer.NameWithWorld);
        ApplyAutomation();
    }
    
    public unsafe void TickOtherPlayerAutomation()
    {
        // Only iterate rendered characters.
        foreach (Character* chara in CharaWatcher.Rendered)
        {
            if ((nint)chara == LocalPlayer.Address)
            {
                continue;
            }

            string nameWorld  = chara->GetNameWithWorld();
            (string Name, Job Job) identifier = (nameWorld, (Job)chara->ClassJob);
            
            // Do logic on unseen players only.
            if (SeenPlayers.Contains(identifier))
            {
                continue;
            }

            // Perform Automation logic.
            PluginLog.Debug($"Begin apply automation for {identifier}");
            
            MyStatusManager mySM = chara->MyStatusManager();

            foreach (AutomationCombo x in chara->GetSuitableAutomation())
            {
                if (!C.SavedPresets.TryGetFirst(a => a.GUID == x.Preset, out var p))
                {
                    continue;
                }

                PluginLog.Debug($"Applied preset {p.ID} / {p.Statuses.Select(z => C.SavedStatuses.FirstOrDefault(s => s.GUID == z)?.Title)}");
                
                mySM.ApplyPreset(p);
            }

            SeenPlayers.Add(identifier);
        }
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
        Safe(() => PurgeEphemeralManagers());
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
