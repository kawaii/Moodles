﻿using Dalamud.Game.Gui.FlyText;
using ECommons.EzHookManager;
using Moodles.Gui;

namespace Moodles;
public unsafe partial class Memory
{
    public delegate void BattleLog_AddToScreenLogWithScreenLogKind(nint target, nint source, FlyTextKind kind, byte a4, byte a5, int a6, int statusID, int stackCount, int damageType);
    [EzHook("48 85 C9 0F 84 ?? ?? ?? ?? 55 56 41 55", false)]
    public EzHook<BattleLog_AddToScreenLogWithScreenLogKind> BattleLog_AddToScreenLogWithScreenLogKindHook;

    void BattleLog_AddToScreenLogWithScreenLogKindDetour(nint target, nint source, FlyTextKind kind, byte a4, byte a5, int a6, int statusID, int stackCount, int damageType)
    {
        try
        {
            PluginLog.Information($"BattleLog_AddActionLogMessageDetour: {target:X16}, {source:X16}, {kind}, {a4}, {a5}, {a6}, {statusID}, {stackCount}, {damageType}");
            if (UI.Suppress) return;
        }
        catch (Exception e)
        {
            e.Log();
        }
        BattleLog_AddToScreenLogWithScreenLogKindHook.Original(target, source, kind, a4, a5, a6, statusID, stackCount, damageType);
    }
}
