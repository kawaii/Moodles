using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using System.Collections.Generic;

namespace Moodles.Moodles.Hooking.Hooks;

internal unsafe class FlyTextHook : CommonMoodleHook
{
    const double FlyTextLifeTime = 1.0;

    readonly List<DateTime> SpawnedFlyTexts = new List<DateTime>();

    delegate void AddToScreenLogWithScreenLogKindDelegate(nint target, nint source, FlyTextKind kind, byte a4, byte a5, int actionID, int statusID, int stackCount, int damageType);

    delegate nint AddFlyText(AddonFlyText* thisPtr, uint actorIndex, uint messageMax, NumberArrayData* numberArrayData, uint offsetNum, uint offsetNumMax, StringArrayData* stringArrayData, uint offsetStr, uint offsetStrMax, int unknown);

    [Signature("48 85 C9 0F 84 ?? ?? ?? ?? 56 41 56", DetourName = nameof(AddToScreenLogWithScreenLogKindDetour))]
    readonly Hook<AddToScreenLogWithScreenLogKindDelegate>? AddToScreenLogWithScreenLogKindHook;

    [Signature("E8 ?? ?? ?? ?? FF C7 41 D1 C7", DetourName = nameof(AddFlyTextDetour))]
    readonly Hook<AddFlyText>? AddFlyTextHook;

    bool lastIsOurs = false;

    IMoodleUser? lastUser;
    IMoodle? lastMoodle;
    bool isAdd = false;

    public FlyTextHook(DalamudServices dalamudServices, IUserList userList, IMoodlesServices moodlesServices, IMoodlesDatabase database) : base(dalamudServices, userList, moodlesServices, database)
    {
        //DalamudServices.Hooking.HookFromAddress<AddonFlyText.Delegates.AddFlyText>((nint)AddonFlyText.StaticVirtualTablePointer, );
    }

    public override void Init()
    {
        AddToScreenLogWithScreenLogKindHook?.Enable();
        AddFlyTextHook?.Enable();

        //DalamudServices.FlyTextGui.FlyTextCreated += OnFlyTextCreated;
    }

    void AddToScreenLogWithScreenLogKindDetour(nint target, nint source, FlyTextKind kind, byte a4, byte a5, int actionID, int statusID, int stackCount, int damageType)
    {
        if (IsHandledByEsuna(target, source, actionID, kind)) return;

        AddToScreenLogWithScreenLogKindHook?.Original(target, source, kind, a4, a5, actionID, statusID, stackCount, damageType);

        PluginLog.LogFatal($"Add screen to log: {MoodlesServices.Sheets.GetStatus((uint)statusID)?.Icon}");
    }

    nint AddFlyTextDetour(AddonFlyText* thisPtr, uint actorIndex, uint messageMax, NumberArrayData* numberArrayData, uint offsetNum, uint offsetNumMax, StringArrayData* stringArrayData, uint offsetStr, uint offsetStrMax, int unknown)
    {
        PluginLog.LogFatal($"ADDED FLYTEXT: {actorIndex}");

        return AddFlyTextHook!.Original(thisPtr, actorIndex, messageMax, numberArrayData, offsetNum, offsetNumMax, stringArrayData, offsetStr, offsetStrMax, unknown);
    }
        
    void OnFlyTextCreated(ref FlyTextKind kind, ref int val1, ref int val2, ref SeString text1, ref SeString text2, ref uint color, ref uint icon, ref uint damageTypeIcon, ref float yOffset, ref bool handled)
    {
        PluginLog.LogFatal($"Flytext: {icon}");

        if (!lastIsOurs)
        {
            return;
        }

        lastIsOurs = false;

        if (lastMoodle != null)
        {
            text1 = Utils.ParseBBSeString((isAdd ? "+ " : "- ") + lastMoodle.Title, true);
        }

        if (lastUser != null)
        {
            text2 = MoodlesServices.StringHelper.AddCasterText(lastUser, text2);
        }

        lastUser = null;
        lastMoodle = null;
    }

    bool IsHandledByEsuna(nint target, nint source, int actionId, FlyTextKind kind)
    {
        if (!MoodlesServices.Configuration.MoodlesCanBeEsunad) return false;

        if (actionId != 7568) return false;
        if (kind != FlyTextKind.HasNoEffect) return false;

        IMoodleUser? uSource = UserList.GetUser(source);
        if (uSource == null) return false;

        if (!MoodlesServices.Configuration.OthersCanEsunaMoodles)
        {
            if (!uSource.IsLocalPlayer) return false;
        }

        IMoodleUser? uTarget = UserList.GetUser(target);
        if (uTarget == null) return false;
        if (uTarget.StatusManager.IsEphemeral) return false;

        int statusCount = uTarget.StatusManager.WorldMoodles.Count;

        for (int i = 0; i < statusCount; i++)
        {
            WorldMoodle wMoodle = uTarget.StatusManager.WorldMoodles[i];

            IMoodle? moodle = Database.GetMoodle(wMoodle);
            if (moodle == null) continue;
            if (!moodle.Dispellable) continue;

            DalamudServices.Framework.RunOnFrameworkThread(() => uTarget.StatusManager.RemoveMoodle(moodle, MoodleReasoning.Esuna, Mediator));
            return true;
        }

        return false;
    }

    bool ValidateSpawnViability()
    {
        for (int i = SpawnedFlyTexts.Count - 1; i >= 0; i--)
        {
            TimeSpan timeSpan = DateTime.Now - SpawnedFlyTexts[i];
            if (timeSpan.TotalSeconds < FlyTextLifeTime) continue;

            SpawnedFlyTexts.RemoveAt(i);
        }

        if (SpawnedFlyTexts.Count >= MoodlesServices.Configuration.MoodleFlyoutTextLimit) return false;

        SpawnedFlyTexts.Add(DateTime.Now);

        return true;
    }

    void SpawnText(nint forAddress, IMoodle moodle, WorldMoodle wMoodle, FlyTextKind kind)
    {
        Status? status = MoodlesServices.Sheets.GetStatusFromIconId((uint)moodle.IconID);
        if (status == null) return;

        if (!ValidateSpawnViability()) return;

        IGameObject? selfObject = DalamudServices.ObjectTable.CreateObjectReference(forAddress);
        if (selfObject == null) return;

        GameObject* gObj = (GameObject*)selfObject.Address;

        nint fromAddress = forAddress;

        IMoodleUser? user = UserList.GetUserFromContentID(wMoodle.AppliedBy);
        if (user != null)
        {
            fromAddress = user.Address;
        }

        lastIsOurs = true;
        lastUser = user;
        lastMoodle = moodle;

        //DalamudServices.FlyTextGui.AddFlyText(kind, gObj->ObjectIndex, 0, 0, moodle.Title, user?.Name ?? string.Empty, 0, (uint)moodle.IconID, 0);

        AddToScreenLogWithScreenLogKindDetour(forAddress, fromAddress, kind, 5, 0, 0, (int)status.Value.RowId, (int)wMoodle.StackCount, 0);
    }

    protected override void OnMoodleApplied(nint forAddress, IMoodle moodle, MoodleReasoning reason, WorldMoodle wMoodle, IMoodleStatusManager statusManager)
    {
        if 
        (
            reason == MoodleReasoning.ManualNoFlag ||
            reason == MoodleReasoning.IPCNoFlag    ||
            reason == MoodleReasoning.Death        ||
            reason == MoodleReasoning.Reflush
        )
        {
            return;
        }

        isAdd = true;
        SpawnText(forAddress, moodle, wMoodle, moodle.StatusType == StatusType.Negative ? FlyTextKind.Debuff : FlyTextKind.Buff);
    }

    protected override void OnMoodleStackChanged(nint forAddress, IMoodle moodle, WorldMoodle wMoodle, IMoodleStatusManager statusManager)
    {
        isAdd = true;
        SpawnText(forAddress, moodle, wMoodle, moodle.StatusType == StatusType.Negative ? FlyTextKind.Debuff : FlyTextKind.Buff);
    }

    protected override void OnMoodleRemoved(nint forAddress, MoodleReasoning reason, IMoodle moodle, WorldMoodle wMoodle, IMoodleStatusManager statusManager)
    {
        if (reason == MoodleReasoning.Reflush) return;

        isAdd = false;
        SpawnText(forAddress, moodle, wMoodle, moodle.StatusType == StatusType.Negative ? FlyTextKind.DebuffFading : FlyTextKind.BuffFading);
    }

    protected override void OnDispose()
    {
        //DalamudServices.FlyTextGui.FlyTextCreated -= OnFlyTextCreated;

        AddFlyTextHook?.Dispose();
        AddToScreenLogWithScreenLogKindHook?.Dispose();
    }
}
