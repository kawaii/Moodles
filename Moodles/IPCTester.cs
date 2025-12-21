using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.EzIpcManager;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Moodles;
#pragma warning disable CS0649, CS8618 // EzIPC makes these warnings like wild but its fine.
public class IPCTester
{
    [EzIPC] private readonly Func<int> Version;

    [EzIPC] private readonly Func<string, string> GetStatusManagerByNameV2;
    [EzIPC] private readonly Func<nint, string> GetStatusManagerByPtrV2;
    [EzIPC] private readonly Func<IPlayerCharacter, string> GetStatusManagerByPlayerV2;

    [EzIPC] private readonly Action<string, string> SetStatusManagerByNameV2;
    [EzIPC] private readonly Action<nint, string> SetStatusManagerByPtrV2;
    [EzIPC] private readonly Action<IPlayerCharacter, string> SetStatusManagerByPlayerV2;

    [EzIPC] private readonly Action<string> ClearStatusManagerByNameV2;
    [EzIPC] private readonly Action<nint> ClearStatusManagerByPtrV2;
    [EzIPC] private readonly Action<IPlayerCharacter> ClearStatusManagerByPlayerV2;

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
                Copy(GetStatusManagerByPlayerV2(pc));
            }
            if(ImGui.Button("Copy (ptr)"))
            {
                Copy(GetStatusManagerByPtrV2(pc.Address));
            }
            if(ImGui.Button("Copy (name)"))
            {
                Copy(GetStatusManagerByNameV2(pc.Name.ToString()));
            }
            if(ImGui.Button("Apply (PC)"))
            {
                SetStatusManagerByPlayerV2(pc, Paste() ?? "");
            }
            if(ImGui.Button("Apply (ptr)"))
            {
                SetStatusManagerByPtrV2(pc.Address, Paste() ?? "");
            }
            if(ImGui.Button("Apply (name)"))
            {
                SetStatusManagerByNameV2(pc.Name.ToString(), Paste() ?? "");
            }
            if(ImGui.Button("Clear (PC)"))
            {
                ClearStatusManagerByPlayerV2(pc);
            }
            if(ImGui.Button("Clear (ptr)"))
            {
                ClearStatusManagerByPtrV2(pc.Address);
            }
            if(ImGui.Button("Clear (name)"))
            {
                ClearStatusManagerByNameV2(pc.Name.ToString());
            }
        }
        else
        {
            ImGuiEx.Text($"Target a player");
        }
    }
}
#pragma warning restore CS0649, CS8618 // EzIPC makes these warnings like wild but its fine.

