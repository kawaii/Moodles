using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
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
        if (ImGui.BeginTable($"##Table", 1, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Borders))
        {
            ImGui.TableHeader($"#h");
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, EColor.RedBright.ToUint());
            ImGuiEx.LineCentered(() => ImGuiEx.Text(EColor.White, "None of this stuff works yet, oh well. :)"));
            ImGui.EndTable();
        }
        P.OtterGuiHandler.Whitelist.Draw(200f);
        ImGui.SameLine();
        using var group = ImRaii.Group();
        DrawHeader();
        DrawSelected();
    }

    private static void DrawHeader()
    {
        HeaderDrawer.Draw(Selected == null ? "Mare Synchronos Global Settings" : (Selected.PlayerName.Censor($"Whitelist entry {C.Whitelist.IndexOf(Selected) + 1}")), 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
    }

    private static void DrawSelected()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child)
            return;

        if (Selected == null)
        {
            ImGui.Checkbox("Allow Moodles From All Players", ref C.BroadcastAllowAll);
            ImGui.Checkbox("Allow Moodles From Friends", ref C.BroadcastAllowFriends);
            ImGui.Checkbox("Allow Moodles From Party Members", ref C.BroadcastAllowParty);
            ImGuiEx.Text($"External Moodle Maximum Duration:");
            ImGuiEx.Spacing();
            Utils.DurationSelector("Any Duration", ref C.BroadcastDefaultEntry.AnyDuration, ref C.BroadcastDefaultEntry.Days, ref C.BroadcastDefaultEntry.Hours, ref C.BroadcastDefaultEntry.Minutes, ref C.BroadcastDefaultEntry.Seconds);
        }
        else
        {
            if (ImGui.BeginTable("##wl", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("##txt", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("##inp", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGuiEx.TextV($"Player Name @ World:");
                ImGui.TableNextColumn();

                ImGuiEx.InputWithRightButtonsArea(() =>
                {
                    ImGui.InputText($"##pname", ref Selected.PlayerName, 100, C.Censor ? ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None);
                }, () =>
                {
                    if (Svc.Targets.Target is PlayerCharacter pc)
                    {
                        if (ImGui.Button("Target"))
                        {
                            Selected.PlayerName = pc.GetNameWithWorld();
                        }
                    }
                });

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Allowed Status Types:");
                if (!Selected.AllowedTypes.Any())
                {
                    ImGuiEx.HelpMarker("No status types selected. Please select at least one status type or no Moodles will be eligible.", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
                ImGui.TableNextColumn();
                foreach (var x in Enum.GetValues<StatusType>())
                {
                    ImGuiEx.CollectionCheckbox($"{x}", x, Selected.AllowedTypes);
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Maximum Duration:");
                if (Selected.TotalMaxDurationSeconds < 1 && !Selected.AnyDuration)
                {
                    ImGuiEx.HelpMarker("No maximum duration configured. Please set one or no Moodles will be eligible.", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
                ImGui.TableNextColumn();

                Utils.DurationSelector("Any Duration", ref Selected.AnyDuration, ref Selected.Days, ref Selected.Hours, ref Selected.Minutes, ref Selected.Seconds);

                ImGui.EndTable();
            }
        }
    }

    static void DrawPresetSelector(AutomationCombo combo)
    {
        var exists = P.OtterGuiHandler.PresetFileSystem.TryGetPathByID(combo.Preset, out var spath);
        if (ImGui.BeginCombo("##addnew", spath ?? "Select preset"))
        {
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputTextWithHint("##search", "Filter", ref Filter, 50);
            foreach (var x in C.SavedPresets)
            {
                if (P.OtterGuiHandler.PresetFileSystem.TryGetPathByID(x.GUID, out var path))
                {
                    if (Filter == "" || path.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                    {
                        var split = path.Split(@"/");
                        var name = split[^1];
                        var directory = split[0..^1].Join(@"/");
                        if (directory != name)
                        {
                            ImGuiEx.RightFloat($"Selector{x.GUID}", () => ImGuiEx.Text(ImGuiColors.DalamudGrey, directory));
                        }
                        if (ImGui.Selectable($"{name}##{x.GUID}", combo.Preset == x.GUID))
                        {
                            combo.Preset = x.GUID;
                        }
                        if (ImGui.IsWindowAppearing() && combo.Preset == x.GUID)
                        {
                            ImGui.SetScrollHereY();
                        }
                    }
                }
            }
            ImGui.EndCombo();
        }
    }
}
