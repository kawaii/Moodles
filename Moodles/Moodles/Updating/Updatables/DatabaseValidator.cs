using Dalamud.Plugin.Services;
using Moodles.Moodles.MoodleUsers;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.Updating.Interfaces;
using System.Linq;

namespace Moodles.Moodles.Updating.Updatables;

internal class DatabaseValidator : IUpdatable
{
    public bool Enabled { get; set; } = true;

    readonly IMoodlesDatabase Database;
    readonly IUserList UserList;

    double counter = 0;
    const int CheckDelay = 300; // 5 minutes

    public DatabaseValidator(IMoodlesDatabase database, IUserList userList)
    {
        Database = database;
        UserList = userList;
    }

    public void Update(IFramework framework)
    {
        counter += framework.UpdateDelta.TotalSeconds;

        if (counter < CheckDelay) return;

        counter -= CheckDelay;

        Verify();
    }

    void Verify()
    {
        PluginLog.LogVerbose("Verify Database");

        foreach (IMoodleStatusManager entry in Database.StatusManagers.ToArray())
        {
            if (!entry.Savable()) continue;

            IMoodleUser? user = UserList.GetUserFromContentID(entry.ContentID);
            if (user != null) continue; // User exists so its fine to keep this IPC user

            PluginLog.LogVerbose($"IPCUser: {entry.ContentID} {entry.SkeletonID} was not found and has been removed from the database.");

            Database.RemoveStatusManager(entry);
        }

        foreach (IMoodle moodle in Database.Moodles.ToArray())
        {
            if (moodle.Savable(Database)) continue;

            PluginLog.LogVerbose($"IMoodle: {moodle.Identifier} {moodle.Title} was not found and has been removed from the database.");

            Database.RemoveMoodle(moodle);
        }
    }
}
