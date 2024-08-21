using Moodles.Data;

namespace Moodles.OtterGuiHandlers.Whitelist.GSpeak;
public class WhitelistMare : WhitelistItemSelectorMare<WhitelistEntryMare>
{
    public WhitelistMare() : base(C.WhitelistMare, Flags.Add | Flags.Delete | Flags.Filter)
    {

    }

    protected override bool OnAdd(string name)
    {
        if (name == "") return false;
        C.WhitelistMare.Add(new() { PlayerName = name });
        return true;
    }

    protected override bool OnDraw(int i)
    {
        var p = C.WhitelistMare[i];
        var ret = ImGui.Selectable($"{p.PlayerName.Censor($"WhitelistEntry {i + 1}")}##{i}", this.CurrentIdx == i);
        return ret;
    }

    protected override bool OnDelete(int idx)
    {
        C.WhitelistMare.RemoveAt(idx);
        return true;
    }

    protected override bool Filtered(int idx)
    {
        var p = C.WhitelistMare[idx];
        return !p.PlayerName.Contains(this.Filter, StringComparison.OrdinalIgnoreCase);
    }
}
