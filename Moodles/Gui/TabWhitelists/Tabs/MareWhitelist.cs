using Moodles.Data;
using Moodles.OtterGuiHandlers;
using OtterGui.Raii;

namespace Moodles.Gui.TabWhitelists.Tabs;

internal class MareWhitelist : PluginWhitelist
{
    WhitelistEntryMare Selected => P.OtterGuiHandler.WhitelistMare.Current;

    public override string pluginName { get; } = "Mare Synchronos";

    protected override void DrawWhitelist()
    {
        if (ImGui.BeginTable($"##Table", 1, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Borders))
        {
            ImGui.TableHeader($"#h");
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, EColor.RedBright.ToUint());
            ImGuiEx.LineCentered(() => ImGuiEx.Text(EColor.White, "None of this stuff works yet, oh well. :)"));
            ImGui.EndTable();
        }

        P.OtterGuiHandler.WhitelistMare.Draw(200f);
    }

    protected override void DrawHeader()
    {
        HeaderDrawer.Draw(Selected == null ? $"{pluginName} Global Settings" : (Selected.PlayerName.Censor($"Whitelist entry {C.WhitelistMare.IndexOf(Selected) + 1}")), 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
    }

    protected override void Draw()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child)
            return;

        // if there are 0 entries in the whitelist, clear the current.
        if (C.WhitelistMare.Count == 0)
        {
            P.OtterGuiHandler.WhitelistMare.EnsureCurrent();
        }

        if (Selected == null)
        {
            ImGuiEx.Text($"No GagSpeak Pairs are visible to view the permissions of. Select one to view permissions!");
        }
        else
        {
            if (ImGui.BeginTable("##wl", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("##txt", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("##inp", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Allowed Status Types:");
                ImGui.TableNextColumn();

                ImGui.BeginDisabled();
                foreach (var x in Enum.GetValues<StatusType>())
                {
                    ImGuiEx.CollectionCheckbox($"{x}", x, Selected.AllowedTypes);
                }
                ImGui.EndDisabled();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Maximum Duration:");
                ImGui.TableNextColumn();

                ImGui.BeginDisabled();
                Utils.DurationSelector("Any Duration", ref Selected.AnyDuration, ref Selected.Days, ref Selected.Hours, ref Selected.Minutes, ref Selected.Seconds);
                ImGui.EndDisabled();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Apply Direction:");
                ImGui.TableNextColumn();

                ImGui.EndTable();
            }
        }
    }
}
