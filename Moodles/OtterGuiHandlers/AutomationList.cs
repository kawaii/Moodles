using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ExcelServices;
using Moodles.Data;
using OtterGui;

namespace Moodles.OtterGuiHandlers;
public class AutomationList : ItemSelector<AutomationProfile>
{
    public AutomationList() : base(C.AutomationProfiles, Flags.Add | Flags.Delete | Flags.Filter)
    {
        
    }

    protected override bool OnAdd(string name)
    {
        if (name == "") return false;
        C.AutomationProfiles.Add(new() { Name = name });
        return true;
    }

    protected override bool OnDraw(int i)
    {
        var p = C.AutomationProfiles[i];
        var col = !p.Enabled;
        if (col)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
        }
        else
        {
            foreach(var x in Svc.Objects)
            {
                if(x is PlayerCharacter pc)
                {
                    if (Utils.GetSuitableAutomation(pc).ContainsAny(p.Combos))
                    {
                        col = true;
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiEx.Vector4FromRGB(0x3fd969));
                    }
                    if (!C.AutoOther) break;
                }
            }
        }
        var cur = ImGui.GetCursorPos();
        var text = $"{p.Character.CensorCharacter()} ({(p.World == 0 ? "Any world" : ExcelWorldHelper.GetName(p.World))})";
        var size = ImGui.CalcTextSize($"{text}");
        ImGui.SetCursorPos(new(190f - size.X, cur.Y + size.Y));
        ImGuiEx.Text($"{text}");
        ImGui.SetCursorPos(cur);
        var ret = ImGui.Selectable($"{p.Name.Censor($"Automation set {i+1}")}\n ##{i}", this.CurrentIdx == i);
        if (col) ImGui.PopStyleColor();
        return ret;
    }

    protected override bool OnDelete(int idx)
    {
        C.AutomationProfiles.RemoveAt(idx);
        return true;
    }

    protected override bool Filtered(int idx)
    {
        var p = C.AutomationProfiles[idx];
        return !p.Character.Contains(this.Filter, StringComparison.OrdinalIgnoreCase);
    }
}
