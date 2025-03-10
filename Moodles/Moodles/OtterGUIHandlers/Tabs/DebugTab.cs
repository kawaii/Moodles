using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.OtterGUIHandlers.Selectors;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.StatusManaging;
using ImGuiNET;

namespace Moodles.Moodles.OtterGUIHandlers.Tabs;

internal class DebugTab
{
    readonly OtterGuiHandler OtterGuiHandler;
    readonly IMoodlesServices Services;
    readonly DalamudServices DalamudServices;
    readonly IMoodlesMediator Mediator;
    readonly IMoodlesDatabase Database;

    public DebugTab(OtterGuiHandler otterGuiHandler, IMoodlesServices services, DalamudServices dalamudServices, IMoodlesDatabase database)
    {
        DalamudServices = dalamudServices;
        OtterGuiHandler = otterGuiHandler;
        Services = services;
        Mediator = services.Mediator;
        Database = database;
    }

    public void Draw()
    {
        ImGui.Text("Saved Moodles:");
        foreach (Moodle savedMoodle in Services.Configuration.SavedMoodles)
        {
            ImGui.Text(savedMoodle.ID);
        }

        ImGui.NewLine(); 
        ImGui.NewLine();

        ImGui.Text("Database Moodles:");
        foreach (IMoodle dbMoodle in Database.Moodles)
        {
            ImGui.Text(dbMoodle.ID);
        }
    }
}
