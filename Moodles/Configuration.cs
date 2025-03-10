using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;
using Newtonsoft.Json;

namespace Moodles;

[Serializable]
internal class Configuration : IPluginConfiguration
{
    [JsonIgnore]
    IDalamudPluginInterface? MoodlesPlugin;
    [JsonIgnore]
    IMoodlesDatabase? Database = null;
    [JsonIgnore]
    bool isSetup = false;

    public int Version { get; set; } = 2;

    public List<Moodle> SavedMoodles = [];
    public List<MoodlesStatusManager> SavedStatusManagers = [];

    public HashSet<uint> FavIcons = [];
    public bool AutoFill = true;
    public bool Censor = false;
    public int SelectorHeight = 33;
    public SortOption IconSortOption = SortOption.Numerical;

    [JsonIgnore] readonly Stopwatch stopwatch = new Stopwatch();

    public void Initialise(IDalamudPluginInterface moodlesPlugin, IMoodlesDatabase database)
    {
        MoodlesPlugin = moodlesPlugin;
        Database = database;
        isSetup = true;
    }

    public void Save()
    {
        if (!isSetup)
        {
            PluginLog.LogFatal("Configuration is NOT setup yet");
            return;
        }

        PluginLog.LogInfo("Saving Moodles");
        stopwatch.Restart();

        Database!.PrepareForSave();

        MoodlesPlugin!.SavePluginConfig(this);

        Database!.CleanupSave();

        PluginLog.LogInfo($"Finished saving moodles in: {stopwatch.Elapsed.TotalSeconds}");
    }
}
