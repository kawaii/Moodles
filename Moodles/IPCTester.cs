using Dalamud.Game.ClientState.Objects.SubKinds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ECommons;
using System.Threading.Tasks;

namespace Moodles;
public class IPCTester : IDisposable
{
    public IPCTester()
    {
        Svc.PluginInterface.GetIpcSubscriber<PlayerCharacter, object>("Moodles.StatusManagerModified").Subscribe(OnStatusManagerModified);
    }

    private void OnStatusManagerModified(PlayerCharacter character)
    {
        PluginLog.Debug($"IPC test: status manager modified {character}");
    }

    public void Draw()
    {
        ImGuiEx.Text($"Version: {Svc.PluginInterface.GetIpcSubscriber<int>("Moodles.Version").InvokeFunc()}");
        if (Svc.Targets.Target is PlayerCharacter pc)
        {
            if (ImGui.Button("Copy (PC)"))
            {
                Copy(Svc.PluginInterface.GetIpcSubscriber<PlayerCharacter, byte[]>("Moodles.GetStatusManagerByPC").InvokeFunc(pc).ToHexString());
            }
            if (ImGui.Button("Copy (ptr)"))
            {
                Copy(Svc.PluginInterface.GetIpcSubscriber<nint, byte[]>("Moodles.GetStatusManagerByPtr").InvokeFunc(pc.Address).ToHexString());
            }
            if (ImGui.Button("Copy (name)"))
            {
                Copy(Svc.PluginInterface.GetIpcSubscriber<string, byte[]>("Moodles.GetStatusManagerByName").InvokeFunc(pc.Name.ToString()).ToHexString());
            }
            if (ImGui.Button("Apply (PC)"))
            {
                TryParseByteArray(Paste(), out var array);
                Svc.PluginInterface.GetIpcSubscriber<PlayerCharacter, byte[], object>("Moodles.SetStatusManagerByPC").InvokeAction(pc, array);
            }
            if (ImGui.Button("Apply (ptr)"))
            {
                TryParseByteArray(Paste(), out var array);
                Svc.PluginInterface.GetIpcSubscriber<nint, byte[], object>("Moodles.SetStatusManagerByPtr").InvokeAction(pc.Address, array);
            }
            if (ImGui.Button("Apply (name)"))
            {
                TryParseByteArray(Paste(), out var array);
                Svc.PluginInterface.GetIpcSubscriber<string, byte[], object>("Moodles.SetStatusManagerByName").InvokeAction(pc.Name.ToString(), array);
            }
            if (ImGui.Button("Clear (PC)"))
            {
                Svc.PluginInterface.GetIpcSubscriber<PlayerCharacter, object>("Moodles.ClearStatusManagerByPC").InvokeAction(pc);
            }
            if (ImGui.Button("Clear (ptr)"))
            {
                Svc.PluginInterface.GetIpcSubscriber<nint, object>("Moodles.ClearStatusManagerByPtr").InvokeAction(pc.Address);
            }
            if (ImGui.Button("Clear (name)"))
            {
                Svc.PluginInterface.GetIpcSubscriber<string, object>("Moodles.ClearStatusManagerByName").InvokeAction(pc.Name.ToString());
            }
        }
        else
        {
            ImGuiEx.Text($"Target a player");
        }
    }

    public void Dispose()
    {
        Svc.PluginInterface.GetIpcSubscriber<PlayerCharacter, object>("Moodles.StatusManagerModified").Unsubscribe(OnStatusManagerModified);
    }
}
