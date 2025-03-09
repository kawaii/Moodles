using ImGuiNET;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.MoodleUsers;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.OtterGUIHandlers;

namespace Moodles.Moodles.TempWindowing.Windows;

internal class MainWindow : MoodleWindow
{
    readonly OtterGuiHandler OtterGuiHandler;
    readonly IMoodlesDatabase Database;
    readonly IUserList UserList;

    public MainWindow(OtterGuiHandler otterGuiHandler, IMoodlesDatabase database, IUserList userList) : base("Moodles", ImGuiWindowFlags.None, true)
    {
        IsOpen = true;

        OtterGuiHandler = otterGuiHandler;
        Database = database;
        UserList = userList;
    }

    public override void Draw()
    {
        
    }
}
