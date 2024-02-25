using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles;
public class IPCProcessor : IDisposable
{
    public IPCProcessor()
    {
        Svc.PluginInterface.GetIpcProvider<int>("Moodles.Version").RegisterFunc(() => 1);

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, byte[]>("Moodles.GetStatusManagerByPC").RegisterFunc(GetStatusManager);
        Svc.PluginInterface.GetIpcProvider<nint, byte[]>("Moodles.GetStatusManagerByPtr").RegisterFunc(GetStatusManager);
        Svc.PluginInterface.GetIpcProvider<string, byte[]>("Moodles.GetStatusManagerByName").RegisterFunc(GetStatusManager);

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, byte[], object>("Moodles.SetStatusManagerByPC").RegisterAction(SetStatusManager);
        Svc.PluginInterface.GetIpcProvider<nint, byte[], object>("Moodles.SetStatusManagerByPtr").RegisterAction(SetStatusManager);
        Svc.PluginInterface.GetIpcProvider<string, byte[], object>("Moodles.SetStatusManagerByName").RegisterAction(SetStatusManager);

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, object>("Moodles.ClearStatusManagerByPC").RegisterAction(ClearStatusManager);
        Svc.PluginInterface.GetIpcProvider<nint, object>("Moodles.ClearStatusManagerByPtr").RegisterAction(ClearStatusManager);
        Svc.PluginInterface.GetIpcProvider<string, object>("Moodles.ClearStatusManagerByName").RegisterAction(ClearStatusManager);

        Svc.PluginInterface.GetIpcProvider<object>("Moodles.Ready").SendMessage();
    }

    public void Dispose()
    {
        Svc.PluginInterface.GetIpcProvider<int>("Moodles.Version").UnregisterFunc();

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, byte[]>("Moodles.GetStatusManagerByPC").UnregisterFunc();
        Svc.PluginInterface.GetIpcProvider<nint, byte[]>("Moodles.GetStatusManagerByPtr").UnregisterFunc();
        Svc.PluginInterface.GetIpcProvider<string, byte[]>("Moodles.GetStatusManagerByName").UnregisterFunc();

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, byte[], object>("Moodles.SetStatusManagerByPC").UnregisterAction();
        Svc.PluginInterface.GetIpcProvider<nint, byte[], object>("Moodles.SetStatusManagerByPtr").UnregisterAction();
        Svc.PluginInterface.GetIpcProvider<string, byte[], object>("Moodles.SetStatusManagerByName").UnregisterAction();

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, object>("Moodles.ClearStatusManagerByPC").UnregisterAction();
        Svc.PluginInterface.GetIpcProvider<nint, object>("Moodles.ClearStatusManagerByPtr").UnregisterAction();
        Svc.PluginInterface.GetIpcProvider<string, object>("Moodles.ClearStatusManagerByName").UnregisterAction();
    }

    void ClearStatusManager(string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.Name.ToString() == name);
        if (obj == null) return;
        ClearStatusManager((PlayerCharacter)obj);
    }
    void ClearStatusManager(nint ptr) => ClearStatusManager((PlayerCharacter)Svc.Objects.CreateObjectReference(ptr));
    void ClearStatusManager(PlayerCharacter pc)
    {
        var m = pc.GetMyStatusManager();
        foreach(var s in m.Statuses)
        {
            if (!s.Persistent)
            {
                m.Cancel(s);
            }
        }
    }

    void SetStatusManager(string name, byte[] data)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.Name.ToString() == name);
        if (obj == null) return;
        SetStatusManager((PlayerCharacter)obj, data);
    }
    void SetStatusManager(nint ptr, byte[] data) => SetStatusManager((PlayerCharacter)Svc.Objects.CreateObjectReference(ptr), data);
    void SetStatusManager(PlayerCharacter pc, byte[] data)
    {
        pc.GetMyStatusManager().DeserializeAndApply(data);
    }

    byte[] GetStatusManager(string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.Name.ToString() == name);
        if (obj == null) return null;
        return GetStatusManager((PlayerCharacter)obj);
    }
    byte[] GetStatusManager(nint ptr) => GetStatusManager((PlayerCharacter)Svc.Objects.CreateObjectReference(ptr));
    byte[] GetStatusManager(PlayerCharacter pc)
    {
        return pc.GetMyStatusManager().BinarySerialize();
    }

    public void FireStatusManagerChange(PlayerCharacter pc)
    {
        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, object>("Moodles.StatusManagerModified").SendMessage(pc);
    }
}
