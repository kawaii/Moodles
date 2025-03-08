using Dalamud.Plugin.Services;
using Moodles.Moodles.Services;
using Moodles.Moodles.Updating.Interfaces;

namespace Moodles.Moodles.Updating.Updatables;

internal class TestUpdatable : IUpdatable
{
    public bool Enabled { get; set; } = true;

    public void Update(IFramework framework)
    {
        PluginLog.Log("Moodles Tick");
    }
}
