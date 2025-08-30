using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.StatusManaging;
using Dalamud.Bindings.ImGui;
using System;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.TempWindowing;
using Moodles.Moodles.Services.Data;

namespace Moodles.Moodles.OtterGUIHandlers.Tabs;

internal class DebugTab
{
    readonly OtterGuiHandler OtterGuiHandler;
    readonly IMoodlesServices Services;
    readonly DalamudServices DalamudServices;
    readonly IMoodlesMediator Mediator;
    readonly IMoodlesDatabase Database;
    readonly IUserList UserList;

    public DebugTab(OtterGuiHandler otterGuiHandler, IMoodlesServices services, DalamudServices dalamudServices, IMoodlesDatabase database, IUserList userList)
    {
        DalamudServices = dalamudServices;
        OtterGuiHandler = otterGuiHandler;
        Services = services;
        Mediator = services.Mediator;
        Database = database;
        UserList = userList;
    }

    public void Draw()
    {
        if (ImGui.Button("Flood some ephemeral"))
        {
            IMoodle newMoodle4 = Database.CreateMoodle(true);
            newMoodle4.SetTitle("MOODLE 4");
            newMoodle4.SetPermanent(false);
            newMoodle4.SetDuration(0, 0, 0, 10);
            newMoodle4.SetIconID(210211);

            IMoodle newMoodle3 = Database.CreateMoodle(true);
            newMoodle3.SetTitle("MOODLE 3");
            newMoodle3.SetStatusOnDispell(newMoodle4.Identifier);
            newMoodle3.SetPermanent(false);
            newMoodle3.SetDuration(0, 0, 0, 10);
            newMoodle3.SetIconID(210211);

            IMoodle newMoodle2 = Database.CreateMoodle(true);
            newMoodle2.SetTitle("MOODLE 2");
            newMoodle2.SetStatusOnDispell(newMoodle3.Identifier);
            newMoodle2.SetPermanent(false);
            newMoodle2.SetDuration(0, 0, 0, 10);
            newMoodle2.SetIconID(210211);

            IMoodle newMoodle1 = Database.CreateMoodle(true);
            newMoodle1.SetTitle("MOODLE 1");
            newMoodle1.SetStatusOnDispell(newMoodle2.Identifier);
            newMoodle1.SetPermanent(false);
            newMoodle1.SetDuration(0, 0, 0, 10);
            newMoodle1.SetIconID(210211);

            newMoodle4.SetStatusOnDispell(newMoodle1.Identifier);

            UserList.LocalPlayer?.StatusManager.ApplyMoodle(newMoodle1, MoodleReasoning.ManualFlag, Services.MoodleValidator, UserList, Services.Mediator);
        }

        foreach (MoodlesStatusManager sm in Database.StatusManagers)
        {
            ImGui.Text($"StatusManager: {sm.ContentID} {sm.SkeletonID}");

            foreach (WorldMoodle moodle in sm.WorldMoodles)
            {
                IMoodle? iMoodle = Database.GetMoodle(moodle);
                if (iMoodle == null)
                {
                    ImGui.Text($"       Moodle: {moodle.Identifier} is a fucking RAT");
                }
                else
                {
                    ImGui.Text($"       Moodle: {moodle.Identifier} [Stacks:{moodle.StackCount}] [Applied By:{moodle.AppliedBy}] {new DateTime(moodle.AppliedOn)} {new DateTime(Services.MoodleValidator.GetMoodleTickTime(moodle, iMoodle))}");
                    ImGui.SameLine();
                    if (ImGui.Button($"X##tempDelete{WindowHandler.InternalCounter}"))
                    {
                        sm.RemoveMoodle(moodle, Moodles.Services.Data.MoodleReasoning.ManualNoFlag, Mediator);
                    }
                }
            }

            ImGui.NewLine();
        }

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
