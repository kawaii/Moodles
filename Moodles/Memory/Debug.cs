using ECommons.EzHookManager;
using Moodles.Gui;

namespace Moodles;
public unsafe partial class Memory
{
    public delegate void ProcessActorControlPacket(uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, int a7, uint a8, long a9, byte a10);
    [EzHook("40 55 53 41 55 41 56 41 57 48 8D AC 24", false)]
    public EzHook<ProcessActorControlPacket> ProcessActorControlPacketHook;
    void ProcessActorControlPacketDetour(uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, int a7, uint a8, long a9, byte a10)
    {
        try
        {
            PluginLog.Information($"ActorControlPacket: {a1:X8}, {a2:X8}, {a3:X8}, {a4:X8}, {a5:X8}, {a6:X8}, {a7:X8}, {a8:X8}, {a9:X16}, {a10:X2}");
            if (UI.Suppress)
            {
                if (a2 == UI.Opcode)
                {
                    PluginLog.Information($"Suppressing");
                    return;
                }
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        ProcessActorControlPacketHook.Original(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10);
    }


    internal delegate void PacketDispatcher_OnReceivePacket(nint a1, uint a2, nint a3);
    [EzHook("40 53 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 8B F2", false)]
    internal EzHook<PacketDispatcher_OnReceivePacket> PacketDispatcher_OnReceivePacketHook;
    void PacketDispatcher_OnReceivePacketDetour(nint a1, uint a2, nint a3)
    {
        try
        {
            var opcode = *(ushort*)(a3 + 2);
            if (UI.Suppress)
            {
                if (opcode == UI.Opcode)
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

    public delegate nint UnkDelegate(nint a1, uint a2, int a3);
    [EzHook("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 50 48 8B F1 41 8B F8 8B CA 8B DA E8 ?? ?? ?? ?? 48 85 C0 74 4C", false)]
    public EzHook<UnkDelegate> UnkDelegateHook;
    public nint UnkDelegateDetour(nint a1, uint a2, int a3)
    {
        try
        {
            PluginLog.Debug($"{a1:X16}, {a2:X8}, {a3}");
            if (UI.Suppress) return 0;
        }
        catch (Exception e)
        {
            e.Log();
        }
        return UnkDelegateHook.Original(a1, a2, a3);
    }

    delegate void AddFlyTextDelegate(IntPtr a1, uint actorIndex, uint a3, IntPtr a4, uint a5, uint a6, IntPtr a7, uint a8, uint a9, int a10);
    [EzHook("E8 ?? ?? ?? ?? FF C7 41 D1 C7", false)]
    EzHook<AddFlyTextDelegate> AddFlyTextHook;
    void AddFlyTextDetour(IntPtr a1, uint actorIndex, uint a3, IntPtr a4, uint a5, uint a6, IntPtr a7, uint a8, uint a9, int a10)
    {
        PluginLog.Debug($"actor: {actorIndex}");
        AddFlyTextHook.Original(a1, actorIndex, a3, a4, a5, a6, a7, a8, a9, a10);
    }


    delegate bool PlaySoundEffect(uint a1, nint a2, nint a3, byte a4);
    [EzHook("E8 ?? ?? ?? ?? 4D 39 BE", false)]
    EzHook<PlaySoundEffect> PlaySoundEffectHook;
    bool PlaySoundEffectDetour(uint a1, nint a2, nint a3, byte a4)
    {
        PluginLog.Debug($"Sound: {a1}");
        return PlaySoundEffectHook.Original(a1, a2, a3, a4);
    }


    internal delegate byte UnkFunc1D(nint a1, byte a2);
    internal UnkFunc1D UnkFunc1 = EzDelegate.Get<UnkFunc1D>("48 83 EC 28 8B 81 ?? ?? ?? ?? 4C 8B C1");

    internal delegate byte UnkFunc2D(nint a1, int a2, byte a3);
    internal UnkFunc2D UnkFunc2 = EzDelegate.Get<UnkFunc2D>("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 81 ?? ?? ?? ?? 41 0F B6 F8");
}
