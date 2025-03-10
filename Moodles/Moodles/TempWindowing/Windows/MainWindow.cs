using ImGuiNET;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.OtterGUIHandlers;
using Moodles.Moodles.OtterGUIHandlers.Tabs;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Services;
using ECommons.ImGuiMethods;

namespace Moodles.Moodles.TempWindowing.Windows;

internal class MainWindow : MoodleWindow
{
    readonly OtterGuiHandler OtterGuiHandler;

    readonly MoodleTab MoodleTab;
    readonly DebugTab DebugTab;
    readonly IMoodlesDatabase Database;

    public MainWindow(OtterGuiHandler otterGuiHandler, DalamudServices dalamudServices, IMoodlesServices services, IMoodlesDatabase database) : base("Moodles", ImGuiWindowFlags.None, true)
    {
        IsOpen = true;

        OtterGuiHandler = otterGuiHandler;
        Database = database;

        MoodleTab = new MoodleTab(OtterGuiHandler, services, dalamudServices, Database);
        DebugTab = new DebugTab(OtterGuiHandler, services, dalamudServices, Database);
    }

    public override void Draw()
    {
        ImGuiEx.EzTabBar
        (
            "##main", 
            [
                ("Moodles", MoodleTab.Draw, null, true),
                ("Debug", DebugTab.Draw, null, true)
            ]
        );
    }
}
