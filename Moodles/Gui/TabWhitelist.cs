using Moodles.Gui.TabWhitelists;
using Moodles.Gui.TabWhitelists.Tabs;

namespace Moodles.Gui;

public static class TabWhitelist
{
    static List<PluginWhitelist> pluginWhitelists = new List<PluginWhitelist>() {
        new MareWhitelist(),
        new GagspeakWhitelist(),
    };

    public static void Draw()
    {
        List<(string name, Action function, Vector4? color, bool child)> tabs = new();

        foreach (var whitelist in pluginWhitelists) 
        {
            tabs.Add((whitelist.pluginName, whitelist.DrawWhitelistTab, null, true));
        }

        ImGuiEx.EzTabBar("##whitelistPluginsSelector", tabs.ToArray());
    }
}
