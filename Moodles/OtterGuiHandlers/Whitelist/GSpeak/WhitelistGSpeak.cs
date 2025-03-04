using Moodles.Data;

namespace Moodles.OtterGuiHandlers.Whitelist.GSpeak;
public class WhitelistGSpeak : WhitelistItemSelectorGSpeak<WhitelistEntryGSpeak>
{
    public WhitelistGSpeak() : base(C.WhitelistGSpeak, Flags.Add | Flags.Delete | Flags.Filter)
    { }

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

    public static void SyncWithGSpeakPlayers(List<(string Name, GSpeakPerms CPFP, GSpeakPerms PPFC)> gSpeakPlayers)
    {
        var currentNames = C.WhitelistGSpeak.Select(entry => entry.PlayerName).ToList();
        // Add new names to the whitelist
        var newEntries = gSpeakPlayers
            .Where(player => !currentNames.Contains(player.Name) && !player.Name.IsNullOrEmpty())
            .Select(player => new WhitelistEntryGSpeak(player.Name, player.CPFP, player.PPFC))
            .ToList();
        C.WhitelistGSpeak.AddRange(newEntries);

        // Update names already present if their permissions do not match
        C.WhitelistGSpeak
            .Where(entry => gSpeakPlayers.Any(player => player.Name == entry.PlayerName))
            .ToList()
            .ForEach(entry =>
            {
                var perms = gSpeakPlayers.First(player => player.Name == entry.PlayerName);
                if(entry.ArePermissionsDifferent(perms.CPFP, perms.PPFC))
                    entry.UpdatePermissions(perms.CPFP, perms.PPFC);
            });

        // Remove names that are no longer in the GSpeakPlayers list
        C.WhitelistGSpeak.RemoveAll(entry => !gSpeakPlayers.Any(player => player.Name == entry.PlayerName));
    }

    public static void ClearGSpeakPlayers()
    {
        C.WhitelistGSpeak.Clear();
    }
}
