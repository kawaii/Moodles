using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.FlyText;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Moodles.Data;
using Moodles.Gui;

namespace Moodles;
public unsafe partial class Memory
{
    // Esuna
    // Medica
    // CastID: 7568

    public delegate void BattleLog_AddToScreenLogWithScreenLogKind(nint target, nint source, FlyTextKind kind, byte a4, byte a5, int actionID, int statusID, int stackCount, int damageType);
    [EzHook("48 85 C9 0F 84 ?? ?? ?? ?? 56 41 56", nameof(BattleLog_AddToScreenLogWithScreenLogKindDetour))]
    public EzHook<BattleLog_AddToScreenLogWithScreenLogKind> BattleLog_AddToScreenLogWithScreenLogKindHook;

    public void BattleLog_AddToScreenLogWithScreenLogKindDetour(nint target, nint source, FlyTextKind kind, byte a4, byte a5, int actionID, int statusID, int stackCount, int damageType)
    {
        try
        {
            if (C.Debug) {
                PluginLog.Debug($"BattleLog_AddActionLogMessageDetour: {target:X16}, {source:X16}, {kind}, {a4}, {a5}, {actionID}, {statusID}, {stackCount}, {damageType}");
            }
            // If Moodles can be Esunad
            if (C.MoodlesCanBeEsunad)
            {
                // If action is Esuna
                if (actionID == 7568 && kind == FlyTextKind.HasNoEffect)
                {
                    // Only perform logic on a dispel if a PlayerCharacter performed it.
                    if (Svc.Objects.CreateObjectReference(source) is IPlayerCharacter pc)
                    {
                        // Check permission
                        if (C.OthersCanEsunaMoodles || pc.ObjectIndex == 0)
                        {
                            // Grab the status manager.
                            if (Utils.GetMyStatusManager(pc) is { } manager && !manager.Ephemeral)
                            {
                                foreach (MyStatus status in manager.Statuses)
                                {
                                    if (!status.Dispelable) continue;
                                    // Ensure only negative statuses are dispelled.
                                    if (status.Type != StatusType.Negative) continue;
                                    // If it can be dispelled and has a dispeller, the name must match.
                                    if (C.OthersCanEsunaMoodles && InvalidDispeller(status, pc)) continue;
                                        
                                    status.ExpiresAt = 0;
                                    // This return is to not show the failed message
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if(UI.Suppress) return;
        }
        catch(Exception e)
        {
            e.Log();
        }
        BattleLog_AddToScreenLogWithScreenLogKindHook.Original(target, source, kind, a4, a5, actionID, statusID, stackCount, damageType);
    }

    private static bool InvalidDispeller(MyStatus status, IPlayerCharacter pc)
        => pc.ObjectIndex is not 0 && (status.Dispeller.Length > 0 && status.Dispeller != pc.GetNameWithWorld());
}
