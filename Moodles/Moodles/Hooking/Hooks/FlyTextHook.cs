using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
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

internal class FlyTextHook : CommonMoodleHook
{
    const double FlyTextLifeTime = 1.0;

    readonly List<DateTime> SpawnedFlyTexts = new List<DateTime>();

    delegate void AddToScreenLogWithScreenLogKindDelegate(nint target, nint source, FlyTextKind kind, byte a4, byte a5, int actionID, int statusID, int stackCount, int damageType);

    [Signature("48 85 C9 0F 84 ?? ?? ?? ?? 56 41 56", DetourName = nameof(AddToScreenLogWithScreenLogKindDetour))]
    readonly Hook<AddToScreenLogWithScreenLogKindDelegate>? AddToScreenLogWithScreenLogKindHook;

    bool lastIsOurs = false;

    IMoodleUser? lastUser;
    IMoodle? lastMoodle;
    bool isAdd = false;

    public FlyTextHook(DalamudServices dalamudServices, IUserList userList, IMoodlesServices moodlesServices, IMoodlesDatabase database) : base(dalamudServices, userList, moodlesServices, database)
    {
        
    }

    public override void Init()
    {
        AddToScreenLogWithScreenLogKindHook?.Enable();

        DalamudServices.FlyTextGui.FlyTextCreated += OnFlyTextCreated;
    }

    void AddToScreenLogWithScreenLogKindDetour(nint target, nint source, FlyTextKind kind, byte a4, byte a5, int actionID, int statusID, int stackCount, int damageType)
    {
        AddToScreenLogWithScreenLogKindHook?.Original(target, source, kind, a4, a5, actionID, statusID, stackCount, damageType);
    }

    void OnFlyTextCreated(ref FlyTextKind kind, ref int val1, ref int val2, ref SeString text1, ref SeString text2, ref uint color, ref uint icon, ref uint damageTypeIcon, ref float yOffset, ref bool handled)
    {
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

        nint fromAddress = forAddress;

        IMoodleUser? user = UserList.GetUserFromContentID(wMoodle.AppliedBy);
        if (user != null)
        {
            fromAddress = user.Address;
        }

        lastIsOurs = true;
        lastUser = user;
        lastMoodle = moodle;

        AddToScreenLogWithScreenLogKindDetour(forAddress, fromAddress, kind, 5, 0, 0, (int)status.Value.RowId, (int)wMoodle.StackCount, 0);
    }

    protected override void OnMoodleApplied(nint forAddress, IMoodle moodle, WorldMoodle wMoodle, IMoodleStatusManager statusManager)
    {
        isAdd = true;
        SpawnText(forAddress, moodle, wMoodle, moodle.StatusType == StatusType.Negative ? FlyTextKind.Debuff : FlyTextKind.Buff);
    }

    protected override void OnMoodleStackChanged(nint forAddress, IMoodle moodle, WorldMoodle wMoodle, IMoodleStatusManager statusManager)
    {
        isAdd = true;
        SpawnText(forAddress, moodle, wMoodle, moodle.StatusType == StatusType.Negative ? FlyTextKind.Debuff : FlyTextKind.Buff);
    }

    protected override void OnMoodleRemoved(nint forAddress, MoodleRemoveReason reason, IMoodle moodle, WorldMoodle wMoodle, IMoodleStatusManager statusManager)
    {
        if (reason == MoodleRemoveReason.Reflush) return;

        isAdd = false;
        SpawnText(forAddress, moodle, wMoodle, moodle.StatusType == StatusType.Negative ? FlyTextKind.DebuffFading : FlyTextKind.BuffFading);
    }

    protected override void OnDispose()
    {
        DalamudServices.FlyTextGui.FlyTextCreated -= OnFlyTextCreated;

        AddToScreenLogWithScreenLogKindHook?.Dispose();
    }
}
