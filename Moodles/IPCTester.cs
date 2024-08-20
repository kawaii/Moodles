using Dalamud.Game.ClientState.Objects.SubKinds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ECommons;
using System.Threading.Tasks;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;

namespace Moodles;
public class IPCTester
{
    [EzIPC] readonly Func<int> Version;
    [EzIPC] readonly Func<IPlayerCharacter, string> GetStatusManagerByPC;
    [EzIPC] readonly Func<nint, string> GetStatusManagerByPtr;
    [EzIPC] readonly Func<string, string> GetStatusManagerByName;
    [EzIPC] readonly Action<IPlayerCharacter, string> SetStatusManagerByPC;
    [EzIPC] readonly Action<nint, string> SetStatusManagerByPtr;
    [EzIPC] readonly Action<string, string> SetStatusManagerByName;
    [EzIPC] readonly Action<IPlayerCharacter> ClearStatusManagerByPC;
    [EzIPC] readonly Action<nint> ClearStatusManagerByPtr;
    [EzIPC] readonly Action<string> ClearStatusManagerByName;

    public IPCTester()
    {
        EzIPC.Init(this, "Moodles");
    }

    [EzIPCEvent]
    private void StatusManagerModified(IPlayerCharacter character)
    {
        PluginLog.Debug($"IPC test: status manager modified {character}");
    }

    [EzIPCEvent]
    private void StatusModified(Guid statusGuid)
    {
         PluginLog.Debug($"IPC test: Moodle status modified {statusGuid}");
    }

    [EzIPCEvent]
    private void PresetModified(Guid presetGuid)
    {
         PluginLog.Debug($"IPC test: Preset status modified {presetGuid}");
    }

    public void Draw()
    {
        // update to use better copy and paste functionality later.
        ImGuiEx.Text($"Version: {Version()}");
        if (Svc.Targets.Target is IPlayerCharacter pc)
        {
            if (ImGui.Button("Copy (PC)"))
            {
                Copy(GetStatusManagerByPC(pc));
            }
            if (ImGui.Button("Copy (ptr)"))
            {
                Copy(GetStatusManagerByPtr(pc.Address));
            }
            if (ImGui.Button("Copy (name)"))
            {
                Copy(GetStatusManagerByName(pc.Name.ToString()));
            }
            if (ImGui.Button("Apply (PC)"))
            {
                SetStatusManagerByPC(pc, Paste());
            }
            if (ImGui.Button("Apply (ptr)"))
            {
                SetStatusManagerByPtr(pc.Address, Paste());
            }
            if (ImGui.Button("Apply (name)"))
            {
                SetStatusManagerByName(pc.Name.ToString(), Paste());
            }
            if (ImGui.Button("Clear (PC)"))
            {
                ClearStatusManagerByPC(pc);
            }
            if (ImGui.Button("Clear (ptr)"))
            {
                ClearStatusManagerByPtr(pc.Address);
            }
            if (ImGui.Button("Clear (name)"))
            {
                ClearStatusManagerByName(pc.Name.ToString());
            }
        }
        else
        {
            ImGuiEx.Text($"Target a player");
        }
    }
}
