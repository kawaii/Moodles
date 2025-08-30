using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Moodles.Moodles.TempWindowing.Interfaces;

namespace Moodles.Moodles.TempWindowing;

internal abstract class MoodleWindow : Window, IMoodleWindow
{
    public MoodleWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(name, flags, forceMainWindow)
    {

    }
}
