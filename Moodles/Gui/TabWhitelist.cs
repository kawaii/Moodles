using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs;
using Moodles.Data;
using Moodles.OtterGuiHandlers;
using OtterGui.Raii;
using System.Data;

namespace Moodles.Gui;
public static class TabWhitelist
{
    static WhitelistEntry Selected => P.OtterGuiHandler.Whitelist.Current;
    static string Filter = "";
    static bool Editing = true;
    public static void Draw()
    {
        P.OtterGuiHandler.Whitelist.Draw(200f);
        ImGui.SameLine();
        using var group = ImRaii.Group();
        DrawHeader();
        DrawSelected();
    }

    private static void DrawHeader()
    {
        HeaderDrawer.Draw(Selected == null ? "GagSpeak Visible Pair Settings" : (Selected.PlayerName.Censor($"Whitelist entry {C.Whitelist.IndexOf(Selected) + 1}")), 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
    }

    private static void DrawSelected()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child)
            return;

        // if there are 0 entries in the whitelist, clear the current.
        if (C.Whitelist.Count == 0)
        {
            P.OtterGuiHandler.Whitelist.EnsureCurrent();
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

                ImGui.BeginDisabled();
                ImGui.Checkbox("Can Apply Our Moodles", ref Selected.CanApplyOurMoodles);
                ImGui.Checkbox("Can Apply Their Moodles", ref Selected.CanApplyTheirMoodles);
                ImGui.EndDisabled();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Status Removal:");
                ImGui.TableNextColumn();
                ImGui.BeginDisabled();
                ImGui.Checkbox("Can Remove Moodles", ref Selected.CanRemoveMoodles);
                ImGui.EndDisabled();
                ImGui.EndTable();
            }
        }
    }
}
