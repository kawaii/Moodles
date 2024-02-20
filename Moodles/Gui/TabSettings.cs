using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.Gui;
public static class TabSettings
{
    public static void Draw()
    {
        ImGui.Checkbox($"Enable Moodles", ref C.Enabled);
        ImGuiEx.CheckboxInverted($"Disable Moodles whilst Bound to Duty", ref C.EnabledDuty);
        ImGuiEx.HelpMarker("Drops all active Moodles from players while you are currently undertaking a Duty, or engaged in battle, such as a levequest or treasure hunt.");
        ImGuiEx.CheckboxInverted($"Disable Moodles whilst in combat", ref C.EnabledCombat);
        ImGuiEx.HelpMarker("Drops all active Moodles from players who are currently in combat, regardless of Duty state.");
        ImGui.Checkbox($"Allow application of Automation Profiles on other player characters", ref C.AutoOther);
        ImGuiEx.HelpMarker("Automation Profiles are computationally expensive and therefore only apply to the local player (you) by default.");
    }
}
