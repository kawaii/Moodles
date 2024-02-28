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

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, string>("Moodles.GetStatusManagerByPC").RegisterFunc(GetStatusManager);
        Svc.PluginInterface.GetIpcProvider<nint, string>("Moodles.GetStatusManagerByPtr").RegisterFunc(GetStatusManager);
        Svc.PluginInterface.GetIpcProvider<string, string>("Moodles.GetStatusManagerByName").RegisterFunc(GetStatusManager);

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, string, object>("Moodles.SetStatusManagerByPC").RegisterAction(SetStatusManager);
        Svc.PluginInterface.GetIpcProvider<nint, string, object>("Moodles.SetStatusManagerByPtr").RegisterAction(SetStatusManager);
        Svc.PluginInterface.GetIpcProvider<string, string, object>("Moodles.SetStatusManagerByName").RegisterAction(SetStatusManager);

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, object>("Moodles.ClearStatusManagerByPC").RegisterAction(ClearStatusManager);
        Svc.PluginInterface.GetIpcProvider<nint, object>("Moodles.ClearStatusManagerByPtr").RegisterAction(ClearStatusManager);
        Svc.PluginInterface.GetIpcProvider<string, object>("Moodles.ClearStatusManagerByName").RegisterAction(ClearStatusManager);

        Svc.PluginInterface.GetIpcProvider<object>("Moodles.Ready").SendMessage();
    }

    public void Dispose()
    {
        Svc.PluginInterface.GetIpcProvider<object>("Moodles.Unloading").SendMessage();

        Svc.PluginInterface.GetIpcProvider<int>("Moodles.Version").UnregisterFunc();

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, string>("Moodles.GetStatusManagerByPC").UnregisterFunc();
        Svc.PluginInterface.GetIpcProvider<nint, string>("Moodles.GetStatusManagerByPtr").UnregisterFunc();
        Svc.PluginInterface.GetIpcProvider<string, string>("Moodles.GetStatusManagerByName").UnregisterFunc();

        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, string, object>("Moodles.SetStatusManagerByPC").UnregisterAction();
        Svc.PluginInterface.GetIpcProvider<nint, string, object>("Moodles.SetStatusManagerByPtr").UnregisterAction();
        Svc.PluginInterface.GetIpcProvider<string, string, object>("Moodles.SetStatusManagerByName").UnregisterAction();

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

    void SetStatusManager(string name, string data)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.Name.ToString() == name);
        if (obj == null) return;
        SetStatusManager((PlayerCharacter)obj, data);
    }
    void SetStatusManager(nint ptr, string data) => SetStatusManager((PlayerCharacter)Svc.Objects.CreateObjectReference(ptr), data);
    void SetStatusManager(PlayerCharacter pc, string data)
    {
        pc.GetMyStatusManager().DeserializeAndApply(Convert.FromBase64String(data));
    }

    string GetStatusManager(string name)
    {
        var obj = Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.GetNameWithWorld() == name);
        obj ??= Svc.Objects.FirstOrDefault(x => x is PlayerCharacter pc && pc.Name.ToString() == name);
        if (obj == null) return null;
        return GetStatusManager((PlayerCharacter)obj);
    }
    string GetStatusManager(nint ptr) => GetStatusManager((PlayerCharacter)Svc.Objects.CreateObjectReference(ptr));
    string GetStatusManager(PlayerCharacter pc)
    {
        return Convert.ToBase64String(pc.GetMyStatusManager().BinarySerialize());
    }

    public void FireStatusManagerChange(PlayerCharacter pc)
    {
        Svc.PluginInterface.GetIpcProvider<PlayerCharacter, object>("Moodles.StatusManagerModified").SendMessage(pc);
    }
}
