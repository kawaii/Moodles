using Moodles.Data;

namespace Moodles.OtterGuiHandlers.Whitelist.GSpeak;
public class ItemSelectorGSpeak : WhitelistItemSelector<WhitelistEntryGSpeak>
{
    public ItemSelectorGSpeak() : base(C.WhitelistGSpeak, Flags.Add | Flags.Delete | Flags.Filter)
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
}
