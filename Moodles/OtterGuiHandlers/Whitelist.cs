using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using Moodles.Data;
using OtterGui;
using System.Windows.Forms;

namespace Moodles.OtterGuiHandlers;
public class Whitelist : WhitelistItemSelector<WhitelistEntry>
{
    public Whitelist() : base(C.Whitelist, Flags.Add | Flags.Delete | Flags.Filter)
    {

    }

    protected override bool OnAdd(string name)
    {
        if (name == "") return false;
        C.Whitelist.Add(new() { PlayerName = name });
        return true;
    }

    protected override bool OnDraw(int i)
    {
        var p = C.Whitelist[i];
        var ret = ImGui.Selectable($"{p.PlayerName.Censor($"WhitelistEntry {i + 1}")}##{i}", this.CurrentIdx == i);
        return ret;
    }

    protected override bool OnDelete(int idx)
    {
        C.Whitelist.RemoveAt(idx);
        return true;
    }

    protected override bool Filtered(int idx)
    {
        var p = C.Whitelist[idx];
        return p != null && !p.PlayerName.Contains(this.Filter, StringComparison.OrdinalIgnoreCase);
    }

    public static void SyncWithGSpeakPlayers(List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)> gSpeakPlayers)
    {
        var currentNames = C.Whitelist.Select(entry => entry.PlayerName).ToList();
        // Add new names to the whitelist
        var newEntries = gSpeakPlayers
            .Where(player => !currentNames.Contains(player.Item1))
            .Select(player =>
            {
                var perms = player.Item3;
                var allowedTypes = new[]
                {
                    (perms.AllowPositive, StatusType.Positive),
                    (perms.AllowNegative, StatusType.Negative),
                    (perms.AllowSpecial, StatusType.Special)
                }
                .Where(t => t.Item1)
                .Select(t => t.Item2)
                .ToList();

                return new WhitelistEntry
                {
                    PlayerName = player.Item1,
                    AllowedTypes = allowedTypes,
                    CanApplyOurMoodles = perms.AllowApplyingPairsMoodles,
                    CanApplyTheirMoodles = perms.AllowApplyingOwnMoodles,
                    Days = perms.MaxDuration.Days,
                    Hours = perms.MaxDuration.Hours,
                    Minutes = perms.MaxDuration.Minutes,
                    Seconds = perms.MaxDuration.Seconds,
                    AnyDuration = perms.AllowPermanent,
                    CanRemoveMoodles = perms.AllowRemoval,
                    ClientPermsForPair = player.Item2
                };
            })
            .ToList();
        // Add the entry range to the whitelist
        C.Whitelist.AddRange(newEntries);

        // Update names already present if their permissions do not match
        C.Whitelist
            .Where(entry => gSpeakPlayers.Any(player => player.Item1 == entry.PlayerName))
            .ToList()
            .ForEach(entry =>
            {
                var perms = gSpeakPlayers.First(player => player.Item1 == entry.PlayerName);
                if (entry.ArePermissionsDifferent(perms.Item2, perms.Item3))
                {
                    entry.UpdatePermissions(perms.Item2, perms.Item3);
                }
            });

        // Remove names that are no longer in the GSpeakPlayers list
        C.Whitelist.RemoveAll(entry => !gSpeakPlayers.Any(player => player.Item1 == entry.PlayerName));
    }
}
