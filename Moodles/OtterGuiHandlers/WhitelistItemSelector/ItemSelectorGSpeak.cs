using Moodles.Data;

namespace Moodles.OtterGuiHandlers.Whitelist.GSpeak;
public class ItemSelectorGSpeak : WhitelistItemSelector<WhitelistEntryGSpeak>
{
    public ItemSelectorGSpeak() : base(IPC.WhitelistGSpeak, Flags.Add | Flags.Delete | Flags.Filter)
    {
        if (Items.Count > 0)
        {
            SetCurrent(Items[0]);
        }
    }

    protected override bool OnAdd(string name)
    {
        if(name == "") return false;
        IPC.WhitelistGSpeak.Add(new());
        return true;
    }

    protected override bool OnDraw(int i)
    {
        var p = IPC.WhitelistGSpeak[i];
        var ret = ImGui.Selectable($"{p.PlayerName.Censor($"WhitelistEntry {i + 1}")}##{i}", CurrentIdx == i);
        return ret;
    }

    protected override bool OnDelete(int idx)
    {
        IPC.WhitelistGSpeak.RemoveAt(idx);
        return true;
    }

    protected override bool Filtered(int idx)
    {
        var p = IPC.WhitelistGSpeak[idx];
        return p != null && !p.PlayerName.Contains(Filter, StringComparison.OrdinalIgnoreCase);
    }
}
