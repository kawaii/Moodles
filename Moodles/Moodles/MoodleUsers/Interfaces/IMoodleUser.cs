using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Collections.Generic;

namespace Moodles.Moodles.MoodleUsers.Interfaces;

internal unsafe interface IMoodleUser : IBattleUser, IMoodleHolder
{
    bool IsActive { get; }
    bool IsLocalPlayer { get; }

    List<IMoodlePet> MoodlePets { get; }

    IMoodlePet? GetPet(nint pet);
    IMoodlePet? GetPet(GameObjectId gameObjectId);
    IMoodlePet? GetYoungestPet(PetFilter filter = PetFilter.None);
    void SetBattlePet(BattleChara* pointer);
    void RemoveBattlePet(BattleChara* pointer);
    void SetCompanion(Companion* companion);
    void RemoveCompanion(Companion* companion);

    enum PetFilter
    {
        None,
        Minion,
        BattlePet,
        Chocobo
    }
}
