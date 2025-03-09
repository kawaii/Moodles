using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.StatusManaging;

namespace Moodles;

[Serializable]
internal class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public List<Moodle> SavedMoodles = [];

    public HashSet<uint> FavIcons = [];
    public bool AutoFill = true;
    public bool Censor = false;
    public int SelectorHeight = 33;
    public SortOption IconSortOption = SortOption.Numerical;

    public void Save(IDalamudPluginInterface plugin)
    {
        plugin.SavePluginConfig(this);
    }
}
