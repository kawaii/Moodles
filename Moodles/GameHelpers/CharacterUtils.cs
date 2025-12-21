using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Lumina.Excel;
using Moodles.Data;
using LuminaWorld = Lumina.Excel.Sheets.World;

namespace Moodles;

// Gives Moodles extensions for Character* that are used commonly by IPlayerCharacter,
// Or used via ECommons.GameHelpers.Player
public static unsafe class CharacterUtils
{
    /// <summary>
    ///     Could be null if not found and create is false.
    /// </summary>
    public static MyStatusManager MyStatusManager(this Character chara, bool create = true)
    {
        return Utils.GetMyStatusManager(chara.GetNameWithWorld(), create);
    }

    // We already know the current players via our watcher.
    public static unsafe nint[] GetTargetablePlayers()
    {
        var list = new List<nint>();
        foreach (Character* chara in CharaWatcher.Rendered)
        {
            if (chara is null) continue;
            if (!chara->GetIsTargetable()) continue;
            // Append to the returns.
            list.Add((nint)chara);
        }
        return list.ToArray();
    }

    public static string GetNameWithWorld(this Character chara)
    {
        return chara.NameString + "@" + (Svc.Data.GetExcelSheet<LuminaWorld>().GetRowOrDefault(chara.HomeWorld) is { } w ? w.Name.ToString() : string.Empty);
    }

    public static IEnumerable<AutomationCombo> GetSuitableAutomation(this Character chara)
    {
        foreach (var x in C.AutomationProfiles)
        {
            if (x.Enabled && x.Character == chara.NameString && (x.World == 0 || x.World == chara.HomeWorld))
            {
                foreach (var c in x.Combos)
                {
                    if (c.Jobs.Count == 0 || c.Jobs.Contains((Job)chara.ClassJob))
                    {
                        yield return c;
                    }
                }
            }
        }
    }

    public static bool CanSpawnVFX(this Character targetChara)
    {
        return true;
    }

    public static bool CanSpawnFlyText(this Character targetChara)
    {
        if (!targetChara.GetIsTargetable()) return false;
        if (!LocalPlayer.Interactable) return false;
        if (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
            || Svc.Condition[ConditionFlag.WatchingCutscene]
            || Svc.Condition[ConditionFlag.WatchingCutscene78]
            || Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
            || Svc.Condition[ConditionFlag.Occupied]
            || Svc.Condition[ConditionFlag.Occupied30]
            || Svc.Condition[ConditionFlag.Occupied33]
            || Svc.Condition[ConditionFlag.Occupied38]
            || Svc.Condition[ConditionFlag.Occupied39]
            || Svc.Condition[ConditionFlag.OccupiedInEvent]
            || Svc.Condition[ConditionFlag.BetweenAreas]
            || Svc.Condition[ConditionFlag.BetweenAreas51]
            || Svc.Condition[ConditionFlag.DutyRecorderPlayback]
            || Svc.Condition[ConditionFlag.LoggingOut]) return false;
        return true;
    }


    /// <summary>
    ///     There are conditions where an object can be rendered / created, but not drawable, or currently bring drawn. <para />
    ///     This mainly occurs on login or when transferring between zones, but can also occur during redraws and such.
    ///     We can get around this by checking for various draw conditions.
    /// </summary>
    public static unsafe bool IsCharaDrawn(Character* character)
    {
        nint addr = (nint)character;
        // Invalid address.
        if (addr == nint.Zero) return false;
        // DrawObject does not exist yet.
        if ((nint)character->DrawObject == nint.Zero) return false;
        // RenderFlags are marked as 'still loading'.
        if ((ulong)character->RenderFlags == 2048) return false;
        // There are models loaded into slots, still being applied.
        if (((CharacterBase*)character->DrawObject)->HasModelInSlotLoaded != 0) return false;
        // There are model files loaded into slots, still being applied.
        if (((CharacterBase*)character->DrawObject)->HasModelFilesInSlotLoaded != 0) return false;
        // Object is fully loaded.
        return true;
    }


    public static RowRef<T> CreateRef<T>(uint rowId) where T : struct, IExcelRow<T>
    {
        return new(Svc.Data.Excel, rowId);
    }


}
