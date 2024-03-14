namespace Moodles.Gui;
public static class TabSettings
{
    public static void Draw()
    {
        ImGui.Checkbox($"Enable Moodles", ref C.Enabled);
        ImGuiEx.Spacing();
        //ImGui.Checkbox("Enable VFX", ref C.EnableVFX);
        ImGui.BeginDisabled();
        var a = false;
        ImGui.Checkbox("Enable VFX", ref a);
        ImGui.EndDisabled();
        ImGuiEx.HelpMarker("VFX features are currently force disabled until a solution to crashing on Mare pair connect/disconnect can be found.");
        ImGuiEx.Spacing();
        ImGui.Checkbox($"Enable Fly/Popup Text", ref C.EnableFlyPopupText);
        ImGuiEx.Spacing();
        ImGuiEx.SetNextItemWidthScaled(150f);
        ImGuiEx.SliderInt($"Simultaneous Fly/Popup Text Limit", ref C.FlyPopupTextLimit.ValidateRange(5, 20), 5, 20);
        ImGuiEx.CheckboxInverted($"Disable Moodles whilst Bound to Duty", ref C.EnabledDuty);
        ImGuiEx.HelpMarker("Hides all active Moodles from players while you are currently undertaking a Duty, or engaged in battle, such as a levequest or treasure hunt.");
        ImGuiEx.CheckboxInverted($"Disable Moodles whilst in combat", ref C.EnabledCombat);
        ImGuiEx.HelpMarker("Hides all active Moodles from players while you are in combat, regardless of Duty state.");
        ImGui.Checkbox($"Allow application of Automation Profiles on other player characters", ref C.AutoOther);
        ImGuiEx.HelpMarker("Automation Profiles are computationally expensive and therefore only apply to the local player (you) by default.");
        ImGuiEx.SetNextItemWidthScaled(150f);
        ImGuiEx.SliderInt($"Icon Selector Scale", ref C.SelectorHeight.ValidateRange(10, 100), 20, 80);
        ImGui.Checkbox($"Display Command Feedback", ref C.DisplayCommandFeedback);
        ImGui.Checkbox($"Debug Mode", ref C.Debug);
    }
}
