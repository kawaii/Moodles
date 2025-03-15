using Moodles.Moodles.Mediation;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.Hooking;

internal abstract class CommonMoodleHook : HookableElement
{
    protected readonly IMoodlesDatabase Database;

    protected CommonMoodleHook(DalamudServices dalamudServices, IUserList userList, IMoodlesServices moodlesServices, IMoodlesDatabase database) : base(dalamudServices, userList, moodlesServices)
    {
        Database = database;

        Mediator.Subscribe<MoodleAppliedMessage>(this, OnMoodleApplied);
        Mediator.Subscribe<MoodleRemovedMessage>(this, OnMoodleRemoved);
        Mediator.Subscribe<MoodleStackChangedMessage>(this, OnMoodleStackChanged);
    }

    protected abstract void OnMoodleApplied(nint forAddress, IMoodle moodle, MoodleReasoning reason, WorldMoodle wMoodle, IMoodleStatusManager statusManager);
    protected abstract void OnMoodleStackChanged(nint forAddress, IMoodle moodle, WorldMoodle wMoodle, IMoodleStatusManager statusManager);
    protected abstract void OnMoodleRemoved(nint forAddress, MoodleReasoning reason, IMoodle moodle, WorldMoodle wMoodle, IMoodleStatusManager statusManager);

    void OnMoodleApplied(MoodleAppliedMessage appliedMessage)
    {
        IMoodleHolder? holder = UserList.GetHolder(appliedMessage.StatusManager.ContentID, appliedMessage.StatusManager.SkeletonID);
        if (holder == null) return;

        OnMoodleApplied(holder.Address, appliedMessage.Moodle, appliedMessage.ApplyReason, appliedMessage.WorldMoodle, appliedMessage.StatusManager);
    }

    void OnMoodleStackChanged(MoodleStackChangedMessage stackChangedMessage)
    {
        IMoodleHolder? holder = UserList.GetHolder(stackChangedMessage.WorldMoodle);
        if (holder == null) return;

        IMoodle? moodle = Database.GetMoodle(stackChangedMessage.WorldMoodle);
        if (moodle == null) return;

        OnMoodleStackChanged(holder.Address, moodle, stackChangedMessage.WorldMoodle, holder.StatusManager);
    }

    void OnMoodleRemoved(MoodleRemovedMessage removedMessage)
    {
        IMoodleHolder? holder = UserList.GetHolder(removedMessage.StatusManager.ContentID, removedMessage.StatusManager.SkeletonID);
        if (holder == null) return;

        IMoodle? moodle = Database.GetMoodle(removedMessage.WorldMoodle);
        if (moodle == null) return;

        OnMoodleRemoved(holder.Address, removedMessage.RemoveReason, moodle, removedMessage.WorldMoodle, holder.StatusManager);
    }
}
