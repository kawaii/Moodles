using Dalamud.Hooking;
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
    private readonly Hook<Companion.Delegates.OnInitialize>?    OnInitializeCompanionHook;
    private readonly Hook<Companion.Delegates.Terminate>?       OnTerminateCompanionHook;
    private readonly Hook<BattleChara.Delegates.OnInitialize>   OnInitializeBattleCharaHook;
    private readonly Hook<BattleChara.Delegates.Terminate>      OnTerminateBattleCharaHook;
    private readonly Hook<BattleChara.Delegates.Dtor>           OnDestroyBattleCharaHook;

    private readonly IMoodlesDatabase Database;

    private readonly List<IntPtr> _temporaryPets = new List<IntPtr>();

    public CharacterManagerHook(DalamudServices services, IUserList userList, IMoodlesServices moodlesServices, IMoodlesDatabase database) : base(services, userList, moodlesServices)
    {
        Database = database;

        OnInitializeCompanionHook   = DalamudServices.Hooking.HookFromAddress<Companion.Delegates.OnInitialize>     ((nint)Companion.StaticVirtualTablePointer->OnInitialize,   InitializeCompanion);
        OnTerminateCompanionHook    = DalamudServices.Hooking.HookFromAddress<Companion.Delegates.Terminate>        ((nint)Companion.StaticVirtualTablePointer->Terminate,      TerminateCompanion);
        OnInitializeBattleCharaHook = DalamudServices.Hooking.HookFromAddress<BattleChara.Delegates.OnInitialize>   ((nint)BattleChara.StaticVirtualTablePointer->OnInitialize, InitializeBattleChara);
        OnTerminateBattleCharaHook  = DalamudServices.Hooking.HookFromAddress<BattleChara.Delegates.Terminate>      ((nint)BattleChara.StaticVirtualTablePointer->Terminate,    TerminateBattleChara);
        OnDestroyBattleCharaHook    = DalamudServices.Hooking.HookFromAddress<BattleChara.Delegates.Dtor>           ((nint)BattleChara.StaticVirtualTablePointer->Dtor,         DestroyBattleChara);
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

            if (bChara == null)
            {
                continue;
            }

            ObjectKind charaKind = bChara->GetObjectKind();

            if (charaKind != ObjectKind.Pc && charaKind != ObjectKind.BattleNpc)
            {
                continue;
            }

            HandleAsCreated(bChara);
        }
    }
    private void InitializeCompanion(Companion* companion)
    {
        try
        {
            OnInitializeCompanionHook!.OriginalDisposeSafe(companion);
        }
        catch (Exception e)
        {
            PluginLog.LogException(e);
        }

        DalamudServices.Framework.Run(() => HandleAsCreatedCompanion(companion));
    }

    private void TerminateCompanion(Companion* companion)
    {
        HandleAsDeletedCompanion(companion);

        try
        {
            OnTerminateCompanionHook!.OriginalDisposeSafe(companion);
        }
        catch (Exception e)
        {
            PluginLog.LogException(e);
        }
    }

    private void InitializeBattleChara(BattleChara* bChara)
    {
        try
        {
            OnInitializeBattleCharaHook!.OriginalDisposeSafe(bChara);
        }
        catch (Exception e)
        {
            PluginLog.LogException(e);
        }

        DalamudServices.Framework.Run(() => HandleAsCreated(bChara));
    }

    private void TerminateBattleChara(BattleChara* bChara)
    {
        HandleAsDeleted(bChara);

        try
        {
            OnTerminateBattleCharaHook!.OriginalDisposeSafe(bChara);
        }
        catch (Exception e)
        {
            PluginLog.LogException(e);
        }
    }

    private GameObject* DestroyBattleChara(BattleChara* bChara, byte freeMemory)
    {
        HandleAsDeleted(bChara);

        try
        {
            return OnDestroyBattleCharaHook!.OriginalDisposeSafe(bChara, freeMemory);
        }
        catch (Exception e)
        {
            PluginLog.LogException(e);
        }

        return null;
    }

    private void HandleAsCreatedCompanion(Companion* companion) => GetOwner(companion)?.SetCompanion(companion);
    private void HandleAsDeletedCompanion(Companion* companion) => GetOwner(companion)?.RemoveCompanion(companion);

    private IMoodleUser? GetOwner(Companion* companion)
    {
        if (companion == null)
        {
            return null;
        }

        return UserList.GetUserFromOwnerID(companion->CompanionOwnerId);
    }

    private void HandleAsCreated(BattleChara* newBattleChara)
    {
        if (newBattleChara == null)
        {
            return;
        }

        ObjectKind actualObjectKind = newBattleChara->ObjectKind;

        if (actualObjectKind == ObjectKind.Pc)
        {
            CreateUser(newBattleChara);
        }

        if (actualObjectKind == ObjectKind.BattleNpc)
        {
            uint owner = newBattleChara->OwnerId;

            for (int i = 0; i < UserList.Users.Length; i++)
            {
                IMoodleUser? user = UserList.Users[i];

                if (user == null)
                {
                    continue;
                }

                if (user.ShortObjectID != owner)
                {
                    continue;
                }

                user.SetBattlePet(newBattleChara);

                break;
            }
        }
    }

    private void HandleAsDeleted(BattleChara* newBattleChara)
    {
        if (newBattleChara == null)
        {
            return;
        }

        nint addressChara = (nint)newBattleChara;

        ObjectKind actualObjectKind = newBattleChara->ObjectKind;

        if (actualObjectKind == ObjectKind.Pc)
        {
            for (int i = 0; i < UserList.Users.Length; i++)
            {
                IMoodleUser? user = UserList.Users[i];
                if (user == null)
                {
                    continue;
                }

                if (user.Address != (nint)newBattleChara)
                {
                    continue;
                }

                user?.Dispose();
                UserList.Users[i] = null;

                break;
            }
        }

        if (actualObjectKind == ObjectKind.BattleNpc)
        {
            _temporaryPets.Remove(addressChara);

            IMoodleUser? user = UserList.GetUser(addressChara);

            if (user == null)
            {
                return;
            }

            user.SetBattlePet(newBattleChara);
        }
    }

    private void AddTempPetsToUser(IMoodleUser user)
    {
        uint userID = user.ShortObjectID;

        for (int i = _temporaryPets.Count - 1; i >= 0; i--)
        {
            nint tempPetPtr = _temporaryPets[i];

            if (tempPetPtr == 0)
            {
                continue;
            }

            BattleChara* tempPet = (BattleChara*)tempPetPtr;

            if (tempPet == null)
            {
                continue;
            }

            if (tempPet->OwnerId != userID)
            {
                continue;
            }

            user.SetBattlePet(tempPet);

            _temporaryPets.RemoveAt(i);
        }
    }

    private IMoodleUser? CreateUser(BattleChara* newBattleChara)
    {
        int actualIndex = CreateActualIndex(newBattleChara->ObjectIndex);

        if (actualIndex < 0 || actualIndex >= 100)
        {
            return null;
        }

        IMoodleUser newUser = new MoodleUser(MoodlesServices, Database, newBattleChara);

        UserList.Users[actualIndex] = newUser;

        AddTempPetsToUser(newUser);

        if (newBattleChara->CompanionData.CompanionObject != null)
        {
            newUser.SetCompanion(newBattleChara->CompanionData.CompanionObject);
        }

        return newUser;
    }

    private int CreateActualIndex(ushort index)
    {
        return (int)MathF.Floor(index * 0.5f);
    }

    protected override void OnDispose()
    {
        OnInitializeCompanionHook?.Dispose();
        OnTerminateCompanionHook?.Dispose();
        OnInitializeBattleCharaHook?.Dispose();
        OnTerminateBattleCharaHook?.Dispose();
        OnDestroyBattleCharaHook?.Dispose();
    }
}
