using ImGuiNET;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.OtterGUIHandlers;
using Moodles.Moodles.OtterGUIHandlers.Tabs;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Mediation.Interfaces;
using Dalamud.Interface.Colors;
using ECommons.ImGuiMethods;
using ECommons.Logging;

namespace Moodles.Moodles.TempWindowing.Windows;

internal class MainWindow : MoodleWindow
{
    readonly OtterGuiHandler OtterGuiHandler;

    readonly MoodleTab MoodleTab;

    public MainWindow(OtterGuiHandler otterGuiHandler, DalamudServices dalamudServices, IMoodlesServices services) : base("Moodles", ImGuiWindowFlags.None, true)
    {
        IsOpen = true;

        OtterGuiHandler = otterGuiHandler;

        MoodleTab = new MoodleTab(OtterGuiHandler, services, dalamudServices);
    }

    public override void Draw()
    {
        ImGuiEx.EzTabBar
        (
            "##main", 
            [
                ("Moodles", MoodleTab.Draw, null, true),
            ]
        );
    }
}
