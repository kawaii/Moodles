using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Moodles.Moodles.StatusManaging;

namespace Moodles;

[Serializable]
internal class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public List<Moodle> SavedMoodles = [];

    public bool Censor = false;

    public void Save(IDalamudPluginInterface plugin)
    {
        plugin.SavePluginConfig(this);
    }
}
