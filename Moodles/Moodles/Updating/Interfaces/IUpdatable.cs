using Dalamud.Plugin.Services;

namespace Moodles.Moodles.Updating.Interfaces;

internal interface IUpdatable
{
    bool Enabled { get; set; }
    void Update(IFramework framework);
}
