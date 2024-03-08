using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ExcelServices;
using Moodles.Data;
using OtterGui;

namespace Moodles.OtterGuiHandlers;
public class Whitelist : ItemSelector<WhitelistEntry>
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
        return !p.PlayerName.Contains(this.Filter, StringComparison.OrdinalIgnoreCase);
    }
}
