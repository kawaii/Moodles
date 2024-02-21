using Dalamud.Game.Gui.FlyText;
using ECommons.EzHookManager;
using Moodles.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles;
public unsafe partial class Memory
{
    public delegate void BattleLog_AddToScreenLogWithScreenLogKind(nint target, nint source,FlyTextKind kind,byte a4, byte a5,int a6,int statusID,int a8,int a9);
    [EzHook("48 85 C9 0F 84 ?? ?? ?? ?? 55 56 41 55", false)]
    public EzHook<BattleLog_AddToScreenLogWithScreenLogKind> BattleLog_AddToScreenLogWithScreenLogKindHook;

    void BattleLog_AddToScreenLogWithScreenLogKindDetour(nint a1, nint a2, FlyTextKind a3, byte a4, byte a5, int a6, int a7, int a8, int a9)
    {
        try
        {
            PluginLog.Information($"BattleLog_AddActionLogMessageDetour: {a1:X16}, {a2:X16}, {a3}, {a4}, {a5}, {a6}, {a7}, {a8}, {a9}");
            if (UI.Suppress) return;
        }
        catch (Exception e)
        {
            e.Log();
        }
        BattleLog_AddToScreenLogWithScreenLogKindHook.Original(a1, a2, a3, a4, a5, a6, a7, a8, a9);
    }
}
