using ImGuiNET;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using System.Collections.Generic;

namespace Moodles.Moodles.TempWindowing.Windows;

internal class DebugWindow : MoodleWindow
{
    readonly IMoodlesDatabase Database;
    readonly IUserList UserList;

    int currentActive = 0;
    readonly List<DevStruct> devStructList = new List<DevStruct>();

    public DebugWindow(IMoodlesDatabase database, IUserList userList) : base("Moodle Debug", ImGuiWindowFlags.None, true)
    {
        IsOpen = true;

        Database = database;
        UserList = userList;

        devStructList.Add(new DevStruct("Moodles", DrawMoodles));
        devStructList.Add(new DevStruct("User List", DrawUserList));
        devStructList.Add(new DevStruct("Database", DrawDatabase));
    }

    void DrawMoodles()
    {
        if (ImGui.Button($"+##+{WindowHandler.InternalCounter}"))
        {
            Database.CreateMoodle();
        }

        IMoodle[] moodles = Database.Moodles;

        foreach (IMoodle moodle in moodles)
        {
            DrawMoodle(moodle);
        }
    }

    void DrawMoodle(IMoodle moodle)
    {
        if (ImGui.Button($"-##-{WindowHandler.InternalCounter}"))
        {
            Database.RemoveMoodle(moodle);
        }

        ImGui.LabelText(moodle.Title, "Title:");
        ImGui.LabelText(moodle.Description, "Description:");
        ImGui.LabelText(moodle.ID, "Guid:");
    }

    void DrawUserList()
    {

    }

    void DrawDatabase()
    {
        IMoodleStatusManager[] entries = Database.StatusManagers;

        foreach (IMoodleStatusManager entry in entries)
        {
            DrawDatabaseUser(entry);
        }
    }

    void DrawDatabaseUser(IMoodleStatusManager user)
    {
        if (!ImGui.BeginTable($"##usersTable{WindowHandler.InternalCounter}", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingMask))
            return;

        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(user.IsActive ? "O" : "X");

        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted($"{user.Name}");

        ImGui.TableSetColumnIndex(2);
        ImGui.TextUnformatted(user.Homeworld.ToString());

        ImGui.TableSetColumnIndex(3);
        ImGui.TextUnformatted(user.IsEphemeral ? "O" : "X");

        ImGui.TableSetColumnIndex(4);
        ImGui.TextUnformatted(UserList.GetUserFromContentID(user.ContentID) != null ? "O" : "X");

        ImGui.EndTable();
    }

    public override void Draw()
    {
        if (devStructList.Count == 0) return;

        ImGui.BeginTabBar("##DevTabBar");

        for (int i = 0; i < devStructList.Count; i++)
        {
            if (!ImGui.TabItemButton(devStructList[i].title)) continue;
            int lastActive = currentActive;
            if (lastActive == i) continue;
            currentActive = i;
        }

        devStructList[currentActive].onSelected?.Invoke();

        ImGui.EndTabBar();
    }
}

struct DevStruct
{
    public readonly string title;
    public readonly Action onSelected;

    public DevStruct(string title, Action onSelected)
    {
        this.title = title;
        this.onSelected = onSelected;
    }
}
