﻿using ECommons.Configuration;

namespace Moodles.Data;
public class Config : IEzConfig
{
    public bool Enabled = true;
    public bool EnabledDuty = false;
    public bool EnabledCombat = false;
    public Dictionary<string, MyStatusManager> StatusManagers = [];
    public List<MyStatus> SavedStatuses = [];
    public List<Preset> SavedPresets = [];
    public List<AutomationProfile> AutomationProfiles = [];
    public HashSet<string> SeenCharacters = [];
    public bool Censor = false;
    public bool AutoOther = false;

    public bool EnableSHE = true;
    public bool EnableFlyPopupText = true;
    public bool RestrictSHE = false;
    public int FlyPopupTextLimit = 10;

    public HashSet<uint> FavIcons = [];
    public bool AutoFill = false;
    public int SelectorHeight = 33;
    public bool Debug = false;
    public bool DebugSaves = true;
    public bool DisplayCommandFeedback = true;
    public bool MoodlesCanBeEsunad = true;
    public bool OthersCanEsunaMoodles = true;
    public SortOption IconSortOption = SortOption.Numerical;
    public List<WhitelistEntryGSpeak> WhitelistGSpeak = [];

    public bool BroadcastAllowAll = false;
    public bool BroadcastAllowFriends = false;
    public bool BroadcastAllowParty = false;
    public WhitelistEntryGSpeak BroadcastDefaultEntry = new();
}
