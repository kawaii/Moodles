using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using System.Collections.Generic;
using static Moodles.Moodles.MoodleUsers.Interfaces.IMoodleUser;

namespace Moodles.Moodles.MoodleUsers;

internal unsafe sealed class MoodleUser : IMoodleUser
{
    public bool IsLocalPlayer { get; }

    public List<IMoodlePet> MoodlePets { get; } = new List<IMoodlePet>();

    public unsafe BattleChara* Self { get; }

    public string Name { get; } = "";
    public ushort Homeworld { get; }
    public ulong ContentID { get; }

    public nint Address { get; private set; }
    public ulong ObjectID { get; }
    public uint ShortObjectID { get; }

    public IMoodleStatusManager StatusManager { get; }

    readonly IMoodlesServices MoodlesServices;
    readonly IMoodlesDatabase Database;

    int __tempStatusCountPlaceholder = 0;

    public MoodleUser(IMoodlesServices moodlesServices, IMoodlesDatabase database, BattleChara* battleChara)
    {
        MoodlesServices = moodlesServices;
        Database = database;

        Self = battleChara;
        Address = (nint)Self;
        IsLocalPlayer = Self->ObjectIndex == 0;
        Name = Self->NameString;
        ContentID = Self->ContentId;
        Homeworld = Self->HomeWorld;

        ObjectID = Self->GetGameObjectId();
        ShortObjectID = Self->GetGameObjectId().ObjectId;

        StatusManager = Database.GetPlayerStatusManager(ContentID);
    }

    void CreateNewPet(IMoodlePet pet, int index = -1)
    {
        if (index == -1)
        {
            MoodlePets.Add(pet);
        }
        else
        {
            MoodlePets.Insert(index, pet);
        }
    }

    public IMoodlePet? GetPet(nint pet)
    {
        int petCount = MoodlePets.Count;
        for (int i = 0; i < petCount; i++)
        {
            IMoodlePet pPet = MoodlePets[i];
            if (pPet.PetPointer == pet) return pPet;
        }
        return null;
    }

    public IMoodlePet? GetPet(GameObjectId gameObjectId)
    {
        int petCount = MoodlePets.Count;
        for (int i = 0; i < petCount; i++)
        {
            IMoodlePet pPet = MoodlePets[i];
            if (pPet.ObjectID == (ulong)gameObjectId) return pPet;
        }
        return null;
    }

    public IMoodlePet? GetYoungestPet(PetFilter filter = PetFilter.None)
    {
        // The last pet  in the list is always the youngest
        for (int i = MoodlePets.Count - 1; i >= 0; i--)
        {
            IMoodlePet pPet = MoodlePets[i];
            if (filter != PetFilter.None)
            {
                if (filter != PetFilter.Minion && pPet is MoodleCompanion) continue;
                if (filter != PetFilter.BattlePet && filter != PetFilter.Chocobo && pPet is MoodleBattlePet) continue;
                if (filter == PetFilter.BattlePet && !MoodlesServices.Sheets.IsValidBattlePet(pPet.SkeletonID)) continue;
                if (filter == PetFilter.Chocobo && MoodlesServices.Sheets.IsValidBattlePet(pPet.SkeletonID)) continue;
            }

            return pPet;
        }
        return null;
    }

    public void SetBattlePet(BattleChara* pointer)
    {
        for (int i = MoodlePets.Count - 1; i >= 0; i--)
        {
            IMoodlePet? pet = MoodlePets[i];
            if (pet == null) continue;
            if (pet.PetPointer != (nint)pointer) continue;
            return;
        }

        CreateNewPet(new MoodleBattlePet(pointer, this, Database, MoodlesServices));
    }

    public void RemoveBattlePet(BattleChara* pointer)
    {
        if (pointer == null) return;

        for (int i = MoodlePets.Count - 1; i >= 0; i--)
        {
            IMoodlePet? pet = MoodlePets[i];
            if (pet == null) continue;
            if (pet.PetPointer != (nint)pointer) continue;

            MoodlePets.RemoveAt(i);
        }
    }

    public void SetCompanion(Companion* companion)
    {
        RemoveCompanion(companion);
        CreateNewPet(new MoodleCompanion(companion, this, Database, MoodlesServices), 0);
    }

    public void RemoveCompanion(Companion* companion)
    {
        if (MoodlePets.Count == 0) return;
        if (MoodlePets[0] is not MoodleCompanion pCompanion) return;

        MoodlePets[0].Dispose();
        MoodlePets.RemoveAt(0);
    }

    public void Dispose()
    {
        if (!StatusManager.Savable())
        {
            Database.RemoveStatusManager(StatusManager);
        }

        foreach (IMoodlePet pet in MoodlePets)
        {
            pet.Dispose();
        }
    }
}
