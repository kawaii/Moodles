using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.MoodleUsers;

internal unsafe abstract class BaseMoodlesPet : IMoodlePet
{
    public IMoodleUser Owner { get; }

    public nint PetPointer { get; }
    public ulong ObjectID { get; }
    public ushort Index { get; }
    public string Name { get; } = "";
    public int SkeletonID { get; }

    public IPetSheetData? PetData { get; }
    public IMoodleStatusManager StatusManager { get; }

    readonly IMoodlesServices MoodleServices;
    readonly IMoodlesDatabase Database;

    int __tempStatusCountPlaceholder = 0;

    public BaseMoodlesPet(Character* pet, IMoodleUser owner, IMoodlesDatabase database, IMoodlesServices moodleServices, bool asBattlePet)
    {
        MoodleServices = moodleServices;
        Owner = owner;
        Database = database;

        PetPointer = (nint)pet;

        SkeletonID = pet->ModelContainer.ModelCharaId;
        if (asBattlePet) SkeletonID = -SkeletonID;
        Index = pet->GameObject.ObjectIndex;
        Name = pet->GameObject.NameString;
        ObjectID = pet->GetGameObjectId();
        PetData = moodleServices.Sheets.GetPet(SkeletonID);

        StatusManager = Database.GetPetStatusManager(owner.ContentID, SkeletonID);
    }

    public void Dispose()
    {
        if (!StatusManager.Savable() || !Owner.StatusManager.Savable())
        {
            Database.RemoveStatusManager(StatusManager);
        }
    }
}
