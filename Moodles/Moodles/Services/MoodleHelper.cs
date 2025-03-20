using Moodles.Moodles.Mediation;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.Services;

internal class MoodleHelper : MoodleSubscriber
{
    readonly IMoodlesDatabase Database;
    readonly DalamudServices DalamudServices;
    readonly IMoodlesServices MoodleServices;
    readonly IUserList UserList;

    public MoodleHelper(IMoodlesDatabase dataBase, DalamudServices dalamudServices, IMoodlesServices moodleServices, IUserList userList) : base(moodleServices.Mediator)
    {
        Database = dataBase;
        DalamudServices = dalamudServices;
        MoodleServices = moodleServices;
        UserList = userList;

        Mediator.Subscribe<MoodleRemovedMessage>(this, OnMoodleRemove);
    }

    // Applies next stage moodle when previous stage got removed
    void OnMoodleRemove(MoodleRemovedMessage message)
    {
        IMoodle? mirrorMoodle = Database.GetMoodle(message.WorldMoodle);
        if (mirrorMoodle == null) return;

        IMoodle? nextMoodle = Database.GetMoodleNoCreate(mirrorMoodle.StatusOnDispell);
        if (nextMoodle == null) return;

        MoodleReasoning removeReason = message.RemoveReason;

        if 
        (
            removeReason == MoodleReasoning.ManualNoFlag ||
            removeReason == MoodleReasoning.IPCNoFlag    ||
            removeReason == MoodleReasoning.Death        ||
            removeReason == MoodleReasoning.Reflush
        )
        {
            return;
        }

        IMoodleStatusManager statusManager = message.StatusManager;

        DalamudServices.Framework.Run(() =>
        {
            statusManager.ApplyMoodle(nextMoodle, MoodleReasoning.ManualFlag, MoodleServices.MoodleValidator, UserList, Mediator);
            
            WorldMoodle? wMoodle = statusManager.GetMoodle(nextMoodle);
            if (wMoodle != null)
            {
                wMoodle.AppliedBy = message.WorldMoodle.AppliedBy;
            }
        });
    }
}
