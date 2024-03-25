namespace Moodles.Gui;
public static class TabSettings
{
    public static void Draw()
    {
        ImGui.Checkbox($"Enable Moodles", ref C.Enabled);
        ImGuiEx.Spacing();
        //ImGui.Checkbox("Enable VFX", ref C.EnableVFX);
        ImGui.Checkbox("Enable Moodle VFX", ref C.EnableSHE);
        ImGuiEx.Spacing();
        ImGui.Checkbox("Restrict VFX application to party, friends and nearby players", ref C.RestrictSHE);
        ImGuiEx.HelpMarker("If enabled, VFX only will be played on your friends, party or nearby players (<15 yalms)");
        ImGuiEx.Spacing();
        ImGui.Checkbox($"Enable Fly/Popup Text", ref C.EnableFlyPopupText);
        ImGuiEx.Spacing();
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderInt($"Simultaneous Fly/Popup Text Limit", ref C.FlyPopupTextLimit.ValidateRange(5, 20), 5, 20);
        ImGuiEx.CheckboxInverted($"Disable Moodles whilst Bound to Duty", ref C.EnabledDuty);
        ImGuiEx.HelpMarker("Hides all active Moodles from players while you are currently undertaking a Duty, or engaged in battle, such as a levequest or treasure hunt.");
        ImGuiEx.CheckboxInverted($"Disable Moodles whilst in combat", ref C.EnabledCombat);
        ImGuiEx.HelpMarker("Hides all active Moodles from players while you are in combat, regardless of Duty state.");
        ImGui.Checkbox($"Allow application of Automation Profiles on other player characters", ref C.AutoOther);
        ImGuiEx.HelpMarker("Automation Profiles are computationally expensive and therefore only apply to the local player (you) by default.");
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderInt($"Icon Selector Scale", ref C.SelectorHeight.ValidateRange(10, 100), 20, 80);
        ImGui.Checkbox($"Display Command Feedback", ref C.DisplayCommandFeedback);
        ImGui.Checkbox($"Debug Mode", ref C.Debug);
    }
}
