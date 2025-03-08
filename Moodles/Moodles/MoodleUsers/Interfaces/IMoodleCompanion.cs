using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Moodles.Moodles.MoodleUsers.Interfaces;

internal unsafe interface IMoodleCompanion : IMoodlePet
{
    Companion* Companion { get; }
}
