using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.MoodleUsers;

internal unsafe sealed class MoodleBattlePet : BaseMoodlesPet, IMoodleBattlePet
{
    public BattleChara* BattlePet { get => (BattleChara*)PetPointer; }

    public MoodleBattlePet(BattleChara* battlePet, IMoodleUser owner, IMoodlesDatabase database, IMoodlesServices moodleServices) : base(&battlePet->Character, owner, database, moodleServices, true) { }
}
