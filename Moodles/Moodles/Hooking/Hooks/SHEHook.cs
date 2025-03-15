using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Moodles.Moodles.Mediation;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;

namespace Moodles.Moodles.Hooking.Hooks;

internal class SHEHook : CommonMoodleHook
{
    const double TimeBetweenSHE = 0.25;

    delegate nint SheApplierDelegate(string path, nint target, nint target2, float speed, byte a5, ushort a6, bool a7);

    [Signature("E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 27 B2 01", DetourName = nameof(SheApplierDetour))]
    readonly Hook<SheApplierDelegate>? SheApplierHook;

    DateTime lastAppliedShe = DateTime.Now;

    public SHEHook(DalamudServices dalamudServices, IUserList userList, IMoodlesServices moodlesServices, IMoodlesDatabase database) : base(dalamudServices, userList, moodlesServices, database)
    {
        
    }

    public override void Init()
    {
        SheApplierHook?.Enable();
    }

    protected override void OnDispose()
    {
        SheApplierHook?.Dispose();
    }

    nint SheApplierDetour(string path, nint target, nint target2, float speed, byte a5, ushort a6, bool a7)
    {
        try
        {
            PluginLog.LogVerbose($"SheApplier {path}, {target:X16}, {target2:X16}, {speed}, {a5}, {a6}, {a7}");
        }
        catch (Exception e)
        {
            PluginLog.LogException(e);
        }

        return SheApplierHook!.Original(path, target, target2, speed, a5, a6, a7);
    }

    void SpawnSHE(string path, nint target)
    {
        path = MoodlesServices.StringHelper.GetVfxPath(path);

        TimeSpan timeBetweenShe = (DateTime.Now - lastAppliedShe);

        if (timeBetweenShe.TotalSeconds < TimeBetweenSHE)
        {
            PluginLog.LogVerbose("Tried to spawn SHE but timeout hasn't been passed yet.");
            return;
        }

        lastAppliedShe = DateTime.Now;

        SheApplierDetour(path, target, target, -1, 0, 0, false);
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

        SpawnSHE(moodle.VFXPath, forAddress);
    }

    protected override void OnMoodleStackChanged(nint forAddress, IMoodle moodle, WorldMoodle wMoodle, IMoodleStatusManager statusManager)
    {
        SpawnSHE(moodle.VFXPath, forAddress);
    }

    protected override void OnMoodleRemoved(nint forAddress, MoodleReasoning reason, IMoodle moodle, WorldMoodle wMoodle, IMoodleStatusManager statusManager)
    {
        if (reason == MoodleReasoning.Reflush) return;

        SpawnSHE("dk04ht_canc0h", forAddress);
    }
}
