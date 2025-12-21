using Moodles.Gui.TabWhitelists;
using Moodles.Gui.TabWhitelists.Tabs;

namespace Moodles.Gui;

public static class TabWhitelist
{
    // leaving this here incase more are added in the future.
    private static List<PluginWhitelist> pluginWhitelists = [];

    public static void Draw()
    {
        List<(string name, Action function, Vector4? color, bool child)> tabs = [];
        foreach(var whitelist in pluginWhitelists)
            tabs.Add((whitelist.pluginName, whitelist.DrawWhitelistTab, null, true));
        
        ImGuiEx.EzTabBar("##whitelistPluginsSelector", tabs.ToArray());
    }

    public static void UpdateWhitelists()
    {
        pluginWhitelists = [new GSpeakPluginWhitelist()];
        // For now keep this out of plain sight until release is ready.
        if (IPC.SundouleiaAvailable) pluginWhitelists.Add(new SundouleiaPluginWhitelist());
    }
}
