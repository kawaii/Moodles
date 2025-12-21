using Dalamud.Game.Gui.FlyText;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
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

    public unsafe void BattleLog_AddToScreenLogWithScreenLogKindDetour(nint target, nint source, FlyTextKind kind, byte a4, byte a5, int actionID, int statusID, int stackCount, int damageType)
    {
        try
        {
            if (C.Debug) {
                PluginLog.Verbose($"BattleLog_AddActionLogMessageDetour: {target:X16}, {source:X16}, {kind}, {a4}, {a5}, {actionID}, {statusID}, {stackCount}, {damageType}");
            }
            // If Moodles can be Esunad
            if (C.MoodlesCanBeEsunad)
            {
                // If action is Esuna
                if (actionID == 7568 && kind == FlyTextKind.HasNoEffect)
                {
                    // Only perform logic on a dispel if a PlayerCharacter performed it.
                    if (CharaWatcher.TryGetValue(source, out Character* chara))
                    {
                        // Check permission
                        if (C.OthersCanEsunaMoodles || chara->ObjectIndex == 0)
                        {
                            // Grab the status manager.
                            if (chara->MyStatusManager() is { } manager && !manager.Ephemeral)
                            {
                                foreach (MyStatus status in manager.Statuses)
                                {
                                    bool isClient = chara->ObjectIndex == 0;

                                    // Ensure only negative statuses are dispelled.
                                    if (status.Type != StatusType.Negative) continue;
                                    // If it cannot be dispelled, skip it.
                                    else if (!status.Modifiers.Has(Modifiers.CanDispel)) continue;
                                    // Client cannot dispel locked statuses.
                                    else if (isClient && manager.LockedIds.Contains(status.GUID)) continue;
                                    // Others cannot dispel if they are not whitelisted.
                                    else if (C.OthersCanEsunaMoodles && !isClient && !IsValidDispeller(status, chara)) continue;

                                    // Perform the dispel, expiring the timer. Also apply the chain if desired.
                                    status.ExpiresAt = 0;
                                    if (status.ChainedStatus != Guid.Empty && status.ChainTrigger is ChainTrigger.Dispel)
                                    {
                                        status.ApplyChain = true;
                                    }
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

    private static unsafe bool IsValidDispeller(MyStatus status, Character* chara)
        => status.Dispeller.Length is 0 || status.Dispeller == chara->GetNameWithWorld();

}
