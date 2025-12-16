using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.EzIpcManager;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Moodles;
#pragma warning disable CS0649, CS8618 // EzIPC makes these warnings like wild but its fine.
public class IPCTester
{
    [EzIPC] private readonly Func<int> Version;
    [EzIPC] private readonly Func<IPlayerCharacter, string> GetStatusManagerByPC;
    [EzIPC] private readonly Func<nint, string> GetStatusManagerByPtr;
    [EzIPC] private readonly Func<string, string> GetStatusManagerByName;
    [EzIPC] private readonly Action<IPlayerCharacter, string> SetStatusManagerByPC;
    [EzIPC] private readonly Action<nint, string> SetStatusManagerByPtr;
    [EzIPC] private readonly Action<string, string> SetStatusManagerByName;
    [EzIPC] private readonly Action<IPlayerCharacter> ClearStatusManagerByPC;
    [EzIPC] private readonly Action<nint> ClearStatusManagerByPtr;
    [EzIPC] private readonly Action<string> ClearStatusManagerByName;
    public IPCTester()
    {
        EzIPC.Init(this, "Moodles");
    }

    [EzIPCEvent]
    private unsafe void StatusManagerModified(nint charaPtr)
    {
        Character* chara = (Character*)charaPtr;
        PluginLog.Verbose($"IPC test: status manager modified ({chara->GetNameWithWorld()}): {string.Join(", ", chara->MyStatusManager().Statuses.Select(x => x.Title))}");
    }

    [EzIPCEvent]
    private void StatusUpdated(Guid statusGuid, bool wasDeleted)
    {
        PluginLog.Verbose($"IPC test: Moodle status modified {statusGuid} (Deleted? {wasDeleted})");
    }

    [EzIPCEvent]
    private void PresetUpdated(Guid presetGuid, bool wasDeleted)
    {
        PluginLog.Verbose($"IPC test: Preset status modified {presetGuid} (Deleted? {wasDeleted})");
    }

    public void Draw()
    {
        // update to use better copy and paste functionality later.
        ImGuiEx.Text($"Version: {Version()}");
        if(Svc.Targets.Target is IPlayerCharacter pc)
        {
            if(ImGui.Button("Copy (PC)"))
            {
                Copy(GetStatusManagerByPC(pc));
            }
            if(ImGui.Button("Copy (ptr)"))
            {
                Copy(GetStatusManagerByPtr(pc.Address));
            }
            if(ImGui.Button("Copy (name)"))
            {
                Copy(GetStatusManagerByName(pc.Name.ToString()));
            }
            if(ImGui.Button("Apply (PC)"))
            {
                SetStatusManagerByPC(pc, Paste() ?? "");
            }
            if(ImGui.Button("Apply (ptr)"))
            {
                SetStatusManagerByPtr(pc.Address, Paste() ?? "");
            }
            if(ImGui.Button("Apply (name)"))
            {
                SetStatusManagerByName(pc.Name.ToString(), Paste() ?? "");
            }
            if(ImGui.Button("Clear (PC)"))
            {
                ClearStatusManagerByPC(pc);
            }
            if(ImGui.Button("Clear (ptr)"))
            {
                ClearStatusManagerByPtr(pc.Address);
            }
            if(ImGui.Button("Clear (name)"))
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
#pragma warning restore CS0649, CS8618 // EzIPC makes these warnings like wild but its fine.

