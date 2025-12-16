using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Player;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Moodles;

// The Local Player in accordance to its FFXIVClientStructs and Memory counterparts.
// All calls here can be done off the framework thread.
public static unsafe class LocalPlayer
{
    // Could use GameObjectManager.Instance()->Objects.IndexSorted[0].Value also.
    public static GameObject*   Object      => (GameObject*)BattleChara;
    public static Character*    Character   => (Character*)BattleChara;
    public static BattleChara*  BattleChara => Control.Instance()->LocalPlayer;
    public static nint Address => (nint)BattleChara;
    public static bool Available => Control.Instance()->LocalPlayer is not null;
    public static bool Interactable => Available && Object->GetIsTargetable();

    // Overview (I have not tested PlayerState results yet, but can use FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState to optimize calls a bit.
    public static string Name => Character->NameString ?? string.Empty;
    public static string CharacterName => PlayerState.Instance()->IsLoaded ? PlayerState.Instance()->CharacterNameString : string.Empty;
    public static string NameWithWorld => Character->GetNameWithWorld();
    public static ulong CID => PlayerState.Instance()->ContentId;
    // Could have been simple as new(BattleChara->GetStatusManager()), but they made that internal.
    public static StatusList StatusList => StatusList.CreateStatusListReference((nint)BattleChara->GetStatusManager())!;
    public static Sex Sex => (Sex)PlayerState.Instance()->Sex;

    // Level related.
    public static int Level => Svc.PlayerState.Level;
    public static byte MaxLevel => PlayerState.Instance()->MaxLevel;
    public static bool IsLevelSynced => PlayerState.Instance()->IsLevelSynced;
    public static int SyncedLevel => PlayerState.Instance()->SyncedLevel;

    // Excel related information.
    public static RowRef<Race> Race => Svc.PlayerState.Race;
    public static RowRef<Tribe> Tribe => CharacterUtils.CreateRef<Tribe>(PlayerState.Instance()->Tribe);
    public static RowRef<World> HomeWorld => Svc.PlayerState.HomeWorld;
    public static RowRef<World> CurrentWorld => Svc.PlayerState.CurrentWorld;
    public static RowRef<WorldDCGroupType> HomeDateCenter => HomeWorld.Value.DataCenter;
    public static RowRef<WorldDCGroupType> CurrentDataCenter => CurrentWorld.Value.DataCenter;
    public static RowRef<TerritoryType> Territory => CharacterUtils.CreateRef<TerritoryType>(GameMain.Instance()->CurrentTerritoryTypeId);
    public static RowRef<ClassJob> ClassJob => Svc.PlayerState.ClassJob;
    public static RowRef<OnlineStatus> OnlineStatus => CharacterUtils.CreateRef<OnlineStatus>(BattleChara->OnlineStatus);
    public static RowRef<ContentFinderCondition> ContentFinderCondition => CharacterUtils.CreateRef<ContentFinderCondition>(GameMain.Instance()->CurrentContentFinderConditionId);

    // World Names
    public static ushort HomeWorldId => Control.Instance()->LocalPlayer->HomeWorld;
    public static ushort CurrentWorldId => Control.Instance()->LocalPlayer->CurrentWorld;

    // World IDs
    public static string HomeWorldName => HomeWorld.ValueNullable?.Name.ToString() ?? string.Empty;
    public static string CurrentWorldName => CurrentWorld.ValueNullable?.Name.ToString() ?? string.Empty;
    public static string HomeDataCenterName => HomeWorld.ValueNullable?.DataCenter.ValueNullable?.Name.ToString() ?? string.Empty;
    public static string CurrentDataCenterName => CurrentWorld.ValueNullable?.DataCenter.ValueNullable?.Name.ToString() ?? string.Empty;

    public static bool IsInHomeWorld => Available && CurrentWorld.RowId == HomeWorld.RowId;
    public static bool IsInHomeDC => Available && CurrentWorld.Value.DataCenter.RowId == HomeWorld.Value.DataCenter.RowId;

    // Can add others if desirable, but should be fine for now.

}
