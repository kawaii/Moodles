using OtterGui.Raii;

namespace Moodles.Gui.TabWhitelists;

internal abstract class PluginWhitelist
{
    public abstract string pluginName { get; }

    protected abstract void DrawWhitelist();
    protected abstract void DrawHeader();
    protected abstract void Draw();

    public void DrawWhitelistTab()
    {
        DrawWhitelist();
        ImGui.SameLine();
        using var group = ImRaii.Group();
        DrawHeader();
        Draw();
    }
}
