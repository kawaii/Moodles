using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Moodles.Moodles.MoodleUsers.Interfaces;

internal unsafe interface IMoodleBattlePet : IMoodlePet
{
    BattleChara* BattlePet { get; }
}
