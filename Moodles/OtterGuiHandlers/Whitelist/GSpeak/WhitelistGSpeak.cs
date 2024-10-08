﻿using Moodles.Data;

namespace Moodles.OtterGuiHandlers.Whitelist.GSpeak;
public class WhitelistGSpeak : WhitelistItemSelectorGSpeak<WhitelistEntryGSpeak>
{
    public WhitelistGSpeak() : base(C.WhitelistGSpeak, Flags.Add | Flags.Delete | Flags.Filter)
    {

    }

    protected override bool OnAdd(string name)
    {
        if(name == "") return false;
        C.WhitelistGSpeak.Add(new() { PlayerName = name });
        return true;
    }

    protected override bool OnDraw(int i)
    {
        var p = C.WhitelistGSpeak[i];
        var ret = ImGui.Selectable($"{p.PlayerName.Censor($"WhitelistEntry {i + 1}")}##{i}", CurrentIdx == i);
        return ret;
    }

    protected override bool OnDelete(int idx)
    {
        C.WhitelistGSpeak.RemoveAt(idx);
        return true;
    }

    protected override bool Filtered(int idx)
    {
        var p = C.WhitelistGSpeak[idx];
        return p != null && !p.PlayerName.Contains(Filter, StringComparison.OrdinalIgnoreCase);
    }

    public static void SyncWithGSpeakPlayers(List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)> gSpeakPlayers)
    {
        var currentNames = C.WhitelistGSpeak.Select(entry => entry.PlayerName).ToList();
        // Add new names to the whitelist
        var newEntries = gSpeakPlayers
            .Where(player => !currentNames.Contains(player.Item1) && !player.Item1.IsNullOrEmpty())
            .Select(player =>
            {
                return new WhitelistEntryGSpeak
                {
                    PlayerName = player.Item1,
                    ClientPermsForPair = player.Item2,
                    PairPermsForClient = player.Item3
                };
            })
            .ToList();
        // Add the entry range to the whitelist
        C.WhitelistGSpeak.AddRange(newEntries);

        // Update names already present if their permissions do not match
        C.WhitelistGSpeak
            .Where(entry => gSpeakPlayers.Any(player => player.Item1 == entry.PlayerName))
            .ToList()
            .ForEach(entry =>
            {
                var perms = gSpeakPlayers.First(player => player.Item1 == entry.PlayerName);
                if(entry.ArePermissionsDifferent(perms.Item2, perms.Item3))
                {
                    entry.UpdatePermissions(perms.Item2, perms.Item3);
                }
            });

        // Remove names that are no longer in the GSpeakPlayers list
        C.WhitelistGSpeak.RemoveAll(entry => !gSpeakPlayers.Any(player => player.Item1 == entry.PlayerName));
    }
}
