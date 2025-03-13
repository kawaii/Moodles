using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Moodles.Moodles.MoodleUsers.Interfaces;

internal unsafe interface IBattleUser
{
    BattleChara* Self { get; }

    string Name { get; }
    ushort Homeworld { get; }
    ulong ContentID { get; }

    ulong ObjectID { get; }
    uint ShortObjectID { get; }
}
