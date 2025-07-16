using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.FlyText;
using ECommons.EzHookManager;
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
            if (C.MoodlesCanBeEsunad)
            {
                // If action is Esuna
                if (actionID == 7568 && kind == FlyTextKind.HasNoEffect)
                {
                    bool esunaValid = true;

                    if (!C.OthersCanEsunaMoodles)
                    {
                        if (Svc.Objects.CreateObjectReference(source) is not IPlayerCharacter sourceChara)
                        {
                            esunaValid = false;
                        }
                        else
                        {
                            // Check local player
                            if (sourceChara.ObjectIndex != 0)
                            {
                                esunaValid = false;
                            }
                        }
                    }

                    if (esunaValid)
                    {
                        if (Svc.Objects.CreateObjectReference(target) is IPlayerCharacter playerChara)
                        {
                            MyStatusManager? manager = Utils.GetMyStatusManager(playerChara);
                            if (manager != null)
                            {
                                if (!manager.Ephemeral)
                                {
                                    foreach (MyStatus status in manager.Statuses)
                                    {
                                        if (!status.Dispelable) continue;
                                        if (status.Type != StatusType.Negative) continue;

                                        status.ExpiresAt = 0;
                                        // This return is to not show the failed message
                                        return;
                                    }
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
}
