using Dalamud.Plugin.Services;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.Updating.Interfaces;

namespace Moodles.Moodles.Updating.Updatables;

internal class DatabaseUpdater : IUpdatable
{
    public bool Enabled { get; set; } = true;

    readonly IMoodlesDatabase Database;

    public DatabaseUpdater(IMoodlesDatabase database)
    {
        Database = database;
    }

    public void Update(IFramework framework)
    {
        Database.Update(framework);  
    }
}
