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
        StatusManager.UpdateEntry(this);

        if (Owner.IsLocalPlayer) StatusManager.SetIdentifier(owner.ContentID, SkeletonID, true);
    }

    public void Dispose()
    {
        // Check if it is a companion
        if (SkeletonID > 0)
        {
            Database.RemoveStatusManager(StatusManager);
        }

        if (__tempStatusCountPlaceholder == 0)
        {
            Database.RemoveStatusManager(StatusManager);
        }

        if (!Owner.IsActive)
        {
            Database.RemoveStatusManager(StatusManager);
        }
    }
}
