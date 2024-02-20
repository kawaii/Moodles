using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons.EzHookManager;
using ECommons.Interop;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles;
public unsafe class Memory : IDisposable
{
    public delegate nint AtkComponentIconText_LoadIconByIDDelegate(void* iconText, int iconId);
    public AtkComponentIconText_LoadIconByIDDelegate AtkComponentIconText_LoadIconByID = EzDelegate.Get<AtkComponentIconText_LoadIconByIDDelegate>("E8 ?? ?? ?? ?? 41 8D 47 2E");

    delegate void AtkComponentIconText_ReceiveEvent(nint a1, short a2, nint a3, nint a4, nint a5);
    [EzHook("48 89 5C 24 ?? 55 48 8B EC 48 83 EC 50 44 0F B7 C2")]
    EzHook<AtkComponentIconText_ReceiveEvent> AtkComponentIconText_ReceiveEventHook;

    delegate bool PlaySoundEffect(uint a1, nint a2, nint a3, byte a4);
    [EzHook("E8 ?? ?? ?? ?? 4D 39 BE", false)]
    EzHook<PlaySoundEffect> PlaySoundEffectHook;

    public delegate nint ActorVfxCreateDelegate(string path, nint a2, nint a3, float a4, char a5, ushort a6, char a7);
    public ActorVfxCreateDelegate ActorVfxCreate = EzDelegate.Get<ActorVfxCreateDelegate>("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8");

    public delegate nint ActorVfxRemoveDelegate(nint vfx, char a2);
    public ActorVfxRemoveDelegate ActorVfxRemove;

    delegate void AddFlyTextDelegate(IntPtr a1,uint actorIndex,uint a3,IntPtr a4,uint a5,uint a6,IntPtr a7,uint a8,uint a9,int a10);
    [EzHook("E8 ?? ?? ?? ?? FF C7 41 D1 C7", false)]
    EzHook<AddFlyTextDelegate> AddFlyTextHook;

    public delegate void UnkDelegate(nint a1, nint a2, nint a3, int a4, byte a5);
    [EzHook("85 D2 0F 84 ?? ?? ?? ?? 48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 41 8B F9", false)]
    public EzHook<UnkDelegate> UnkDelegateHook;

    const string PacketDispatcher_OnReceivePacketHookSig = "40 53 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 8B F2";
    internal delegate void PacketDispatcher_OnReceivePacket(nint a1, uint a2, nint a3);
    [EzHook(PacketDispatcher_OnReceivePacketHookSig, false)]
    internal EzHook<PacketDispatcher_OnReceivePacket> PacketDispatcher_OnReceivePacketHook;

    public delegate void ProcessActorControlPacket(uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, int a7, uint a8, long a9, byte a10);
    [EzHook("40 55 53 41 55 41 56 41 57 48 8D AC 24", false)]
    public EzHook<ProcessActorControlPacket> ProcessActorControlPacketHook;

    public Memory()
    {
        EzSignatureHelper.Initialize(this);
        var actorVfxRemoveAddresTemp = Svc.SigScanner.ScanText("0F 11 48 10 48 8D 05") + 7;
        var actorVfxRemoveAddress = Marshal.ReadIntPtr(actorVfxRemoveAddresTemp + Marshal.ReadInt32(actorVfxRemoveAddresTemp) + 4);
        ActorVfxRemove = Marshal.GetDelegateForFunctionPointer<ActorVfxRemoveDelegate>(actorVfxRemoveAddress);
    }

    void ProcessActorControlPacketDetour(uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, int a7, uint a8, long a9, byte a10)
    {
        try
        {
            PluginLog.Information($"ActorControlPacket: {a1:X8}, {a2:X8}, {a3:X8}, {a4:X8}, {a5:X8}, {a6:X8}, {a7:X8}, {a8:X8}, {a9:X16}, {a10:X2}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        ProcessActorControlPacketHook.Original(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10);
    }

    void PacketDispatcher_OnReceivePacketDetour(nint a1, uint a2, nint a3)
    {
        try
        {
            var opcode = *(ushort*)(a3 + 2);
            if (UI.Suppress)
            {
                if (opcode == 248)
                {
                    PluginLog.Information($"Suppressing");
                    return;
                }
            }
        }
        catch (Exception e)
        {
            e.Log();
            return;
        }

        PacketDispatcher_OnReceivePacketHook.Original(a1, a2, a3);
    }

    public void UnkDelegateDetour(nint a1, nint a2, nint a3, int a4, byte a5)
    {
        try
        {
            PluginLog.Debug($"{a1:X16}, {a2:X16}, {a3:X16}, {a4}, {a5}");
            if (UI.Suppress) return;
        }
        catch(Exception e)
        {
            e.Log();
        }
        UnkDelegateHook.Original(a1, a2, a3,a4,a5);
    }

    void AddFlyTextDetour(IntPtr a1, uint actorIndex, uint a3, IntPtr a4, uint a5, uint a6, IntPtr a7, uint a8, uint a9, int a10)
    {
        PluginLog.Debug($"actor: {actorIndex}");
        AddFlyTextHook.Original(a1, actorIndex, a3, a4, a5, a6, a7, a8, a9, a10);
    }

    bool PlaySoundEffectDetour(uint a1, nint a2, nint a3, byte a4)
    {
        PluginLog.Debug($"Sound: {a1}");
        return PlaySoundEffectHook.Original(a1, a2, a3, a4);
    }

    void AtkComponentIconText_ReceiveEventDetour(nint a1, short a2, nint a3, nint a4, nint a5)
    {
        try
        {
            //PluginLog.Debug($"{a1:X16}, {a2}, {a3:X16}, {a4:X16}, {a5:X16}");
            if (a2 == 6)
            {
                P.CommonProcessor.HoveringOver = a1;
            }
            if (a2 == 7)
            {
                P.CommonProcessor.HoveringOver = 0;
            }
            if (a2 == 9 && P.CommonProcessor.WasRightMousePressed)
            {
                P.CommonProcessor.CancelRequests.Add(a1);
                P.CommonProcessor.HoveringOver = 0;
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        AtkComponentIconText_ReceiveEventHook.Original(a1, a2, a3, a4, a5);
    }

    public void Dispose()
    {
        
    }
}
