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
                Copy(Svc.PluginInterface.GetIpcSubscriber<PlayerCharacter, string>("Moodles.GetStatusManagerByPC").InvokeFunc(pc));
            }
            if (ImGui.Button("Copy (ptr)"))
            {
                Copy(Svc.PluginInterface.GetIpcSubscriber<nint, string>("Moodles.GetStatusManagerByPtr").InvokeFunc(pc.Address));
            }
            if (ImGui.Button("Copy (name)"))
            {
                Copy(Svc.PluginInterface.GetIpcSubscriber<string, string>("Moodles.GetStatusManagerByName").InvokeFunc(pc.Name.ToString()));
            }
            if (ImGui.Button("Apply (PC)"))
            {
                Svc.PluginInterface.GetIpcSubscriber<PlayerCharacter, string, object>("Moodles.SetStatusManagerByPC").InvokeAction(pc, Paste());
            }
            if (ImGui.Button("Apply (ptr)"))
            {
                Svc.PluginInterface.GetIpcSubscriber<nint, string, object>("Moodles.SetStatusManagerByPtr").InvokeAction(pc.Address, Paste());
            }
            if (ImGui.Button("Apply (name)"))
            {
                Svc.PluginInterface.GetIpcSubscriber<string, string, object>("Moodles.SetStatusManagerByName").InvokeAction(pc.Name.ToString(), Paste());
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
