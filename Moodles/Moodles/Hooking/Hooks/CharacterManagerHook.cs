using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Moodles.Moodles.MoodleUsers;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using System.Collections.Generic;

namespace Moodles.Moodles.Hooking.Hooks;

internal unsafe class CharacterManagerHook : HookableElement
{
    delegate Companion* Companion_OnInitializeDelegate(Companion* companion);
    delegate Companion* Companion_TerminateDelegate(Companion* companion);
    delegate BattleChara* BattleChara_OnInitializeDelegate(BattleChara* battleChara);
    delegate BattleChara* BattleChara_TerminateDelegate(BattleChara* battleChara);
    delegate BattleChara* BattleChara_DestroyDelegate(BattleChara* battleChara, bool freeMemory);

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 33 FF 48 8B D9 48 89 B9 ?? ?? ?? ?? 66 89 B9 ?? ?? ?? ??", DetourName = nameof(InitializeCompanion))]
    readonly Hook<Companion_OnInitializeDelegate>? OnInitializeCompanionHook = null;

    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 33 ED 48 8D 99 ?? ?? ?? ?? 48 89 A9 ?? ?? ?? ??", DetourName = nameof(TerminateCompanion))]
    readonly Hook<Companion_TerminateDelegate>? OnTerminateCompanionHook = null;

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B D7", DetourName = nameof(InitializeBattleChara))]
    readonly Hook<BattleChara_OnInitializeDelegate>? OnInitializeBattleCharaHook = null;

    [Signature("40 53 48 83 EC 20 8B 91 ?? ?? ?? ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ??", DetourName = nameof(TerminateBattleChara))]
    readonly Hook<BattleChara_TerminateDelegate>? OnTerminateBattleCharaHook = null;

    [Signature("48 89 5C 24 08 57 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B F9 48 89 01 8B DA 48 8D 05 ?? ?? ?? ?? 48 89 81 A0 01 00 00 48 81 C1 90 36 00 00", DetourName = nameof(DestroyBattleChara))]
    readonly Hook<BattleChara_DestroyDelegate>? OnDestroyBattleCharaHook = null;

    readonly IMoodlesDatabase Database;

    readonly List<IntPtr> temporaryPets = new List<IntPtr>();

    public CharacterManagerHook(DalamudServices services, IUserList userList, IMoodlesServices moodlesServices, IMoodlesDatabase database) : base(services, userList, moodlesServices)
    {
        Database = database;
    }

    public override void Init()
    {
        OnInitializeCompanionHook?.Enable();
        OnTerminateCompanionHook?.Enable();
        OnInitializeBattleCharaHook?.Enable();
        OnTerminateBattleCharaHook?.Enable();
        OnDestroyBattleCharaHook?.Enable();

        FloodInitialList();
    }

    void FloodInitialList()
    {
        for (int i = 0; i < 100; i++)
        {
            BattleChara* bChara = CharacterManager.Instance()->BattleCharas[i];
            if (bChara == null) continue;

            ObjectKind charaKind = bChara->GetObjectKind();
            if (charaKind != ObjectKind.Pc && charaKind != ObjectKind.BattleNpc) continue;

            HandleAsCreated(bChara);
        }
    }

    Companion* InitializeCompanion(Companion* companion)
    {
        Companion* initializedCompanion = OnInitializeCompanionHook!.Original(companion);

        DalamudServices.Framework.Run(() => HandleAsCreatedCompanion(companion));

        return initializedCompanion;
    }

    Companion* TerminateCompanion(Companion* companion)
    {
        HandleAsDeletedCompanion(companion);

        return OnTerminateCompanionHook!.Original(companion);
    }

    BattleChara* InitializeBattleChara(BattleChara* bChara)
    {
        BattleChara* initializedBattleChara = OnInitializeBattleCharaHook!.Original(bChara);

        DalamudServices.Framework.Run(() => HandleAsCreated(bChara));

        return initializedBattleChara;
    }

    BattleChara* TerminateBattleChara(BattleChara* bChara)
    {
        HandleAsDeleted(bChara);

        return OnTerminateBattleCharaHook!.Original(bChara);
    }

    BattleChara* DestroyBattleChara(BattleChara* bChara, bool freeMemory)
    {
        HandleAsDeleted(bChara);

        return OnDestroyBattleCharaHook!.Original(bChara, freeMemory);
    }

    void HandleAsCreatedCompanion(Companion* companion) => GetOwner(companion)?.SetCompanion(companion);
    void HandleAsDeletedCompanion(Companion* companion) => GetOwner(companion)?.RemoveCompanion(companion);

    IMoodleUser? GetOwner(Companion* companion)
    {
        if (companion == null) return null;

        return UserList.GetUserFromOwnerID(companion->CompanionOwnerId);
    }

    void HandleAsCreated(BattleChara* newBattleChara)
    {
        if (newBattleChara == null) return;

        ObjectKind actualObjectKind = newBattleChara->ObjectKind;

        if (actualObjectKind == ObjectKind.Pc)
        {
            CreateUser(newBattleChara);
        }

        if (actualObjectKind == ObjectKind.BattleNpc)
        {
            uint owner = newBattleChara->OwnerId;

            bool gotOwner = false;

            for (int i = 0; i < UserList.Users.Length; i++)
            {
                IMoodleUser? user = UserList.Users[i];
                if (user == null) continue;
                if (user.ShortObjectID != owner) continue;

                user.SetBattlePet(newBattleChara);
                gotOwner = true;
                break;
            }

            if (!gotOwner)
            {
                temporaryPets.Add((nint)newBattleChara);
            }
        }
    }

    void HandleAsDeleted(BattleChara* newBattleChara)
    {
        if (newBattleChara == null) return;

        nint addressChara = (nint)newBattleChara;

        ObjectKind actualObjectKind = newBattleChara->ObjectKind;

        if (actualObjectKind == ObjectKind.Pc)
        {
            for (int i = 0; i < UserList.Users.Length; i++)
            {
                IMoodleUser? user = UserList.Users[i];
                if (user == null) continue;
                if (user.Self != newBattleChara) continue;

                user?.Dispose();
                UserList.Users[i] = null;
                break;
            }
        }

        if (actualObjectKind == ObjectKind.BattleNpc)
        {
            temporaryPets.Remove(addressChara);

            IMoodleUser? user = UserList.GetUser(addressChara);
            if (user == null) return;

            user.RemoveBattlePet(newBattleChara);
        }
    }

    void AddTempPetsToUser(IMoodleUser user)
    {
        uint userID = user.ShortObjectID;

        for (int i = temporaryPets.Count - 1; i >= 0; i--)
        {
            nint tempPetPtr = temporaryPets[i];
            if (tempPetPtr == 0) continue;

            BattleChara* tempPet = (BattleChara*)tempPetPtr;
            if (tempPet == null) continue;
            if (tempPet->OwnerId != userID) continue;

            user.SetBattlePet(tempPet);
            temporaryPets.RemoveAt(i);
        }
    }

    void CreateUser(BattleChara* newBattleChara)
    {
        if (newBattleChara == null) return;
        if (newBattleChara->HomeWorld == ushort.MaxValue) return;

        IMoodleUser? newUser = new MoodleUser(MoodlesServices, Database, newBattleChara);

        int actualIndex = CreateActualIndex(newBattleChara->ObjectIndex);
        if (actualIndex < 0 || actualIndex >= 100) return;

        UserList.Users[actualIndex] = newUser;

        AddTempPetsToUser(newUser);

        if (newBattleChara->CompanionData.CompanionObject != null)
        {
            newUser.SetCompanion(newBattleChara->CompanionData.CompanionObject);
        }
    }

    int CreateActualIndex(ushort index) => (int)MathF.Floor(index * 0.5f);

    protected override void OnDispose()
    {
        OnInitializeCompanionHook?.Dispose();
        OnTerminateCompanionHook?.Dispose();
        OnInitializeBattleCharaHook?.Dispose();
        OnTerminateBattleCharaHook?.Dispose();
        OnDestroyBattleCharaHook?.Dispose();
    }
}
