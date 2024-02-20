using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using Moodles.Data;
using Moodles.OtterGuiHandlers;
using OtterGui.Raii;
using System.Data;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace Moodles.Gui;
public static class TabAutomation
{
    public static Vector2 JobIconSize => new Vector2(24f.Scale(), 24f.Scale());
    static AutomationProfile Selected => P.OtterGuiHandler.AutomationList.Current;
    static string Filter = "";
    static bool Editing = true;
    public static void Draw()
    {
        P.OtterGuiHandler.AutomationList.Draw(200f);
        ImGui.SameLine();
        using var group = ImRaii.Group();
        DrawHeader();
        DrawSelected();
    }

    private static void DrawHeader()
    {
        HeaderDrawer.Draw(Selected == null?"":(Selected.Name.Censor($"Automation set {C.AutomationProfiles.IndexOf(Selected)+1}")), 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
    }

    private static void DrawSelected()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || Selected == null)
            return;

        if (ImGui.Checkbox("Enabled", ref Selected.Enabled))
        {
            if (Selected.Enabled) P.ApplyAutomation(true);
        }
        ImGui.SameLine();
        ImGui.Checkbox("Show Editing", ref Editing);
        ImGui.SameLine();
        if (ImGui.Button("Reapply Automation"))
        {
            P.ApplyAutomation(true);
        }

        ImGui.Separator();

        if (Editing)
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 150);
            ImGui.InputText($"Rename Set", ref Selected.Name, 100, C.Censor?ImGuiInputTextFlags.Password:ImGuiInputTextFlags.None);

            ImGui.SetNextItemWidth(120);
            if (ImGui.BeginCombo($"##world", Selected.World == 0?"Any world" : ExcelWorldHelper.GetName(Selected.World)))
            {
                if (ImGui.Selectable("Any world")) Selected.World = 0;
                foreach(var x in ExcelWorldHelper.GetPublicWorlds(null).OrderBy(z => z.Name.ToString()))
                {
                    if (ImGui.Selectable(x.Name)) Selected.World = x.RowId;
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 150);
            ImGui.InputTextWithHint("##cname", "Character name", ref Selected.Character, 50, C.Censor ? ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None);

            var buttonSize = new Vector2((ImGui.GetContentRegionAvail().X - 150) / 2 - ImGui.GetStyle().ItemSpacing.X / 2, ImGuiHelpers.GetButtonSize(" ").Y);

            {
                var dis = !Player.Available;
                if (dis) ImGui.BeginDisabled();
                if (ImGui.Button("Set to Character", buttonSize))
                {
                    Selected.World = Player.Object.HomeWorld.Id;
                    Selected.Character = Player.Name;
                }
                if (dis) ImGui.EndDisabled();
            }
            ImGui.SameLine();
            {
                var dis = Svc.Targets.Target is not PlayerCharacter;
                if (dis) ImGui.BeginDisabled();
                if (ImGui.Button("Set to Target", buttonSize))
                {
                    Selected.World = ((PlayerCharacter)Svc.Targets.Target).HomeWorld.Id;
                    Selected.Character = ((PlayerCharacter)Svc.Targets.Target).Name.ToString();
                }
                if (dis) ImGui.EndDisabled();
            }
        }

        if (Selected.Combos.Count == 0) Selected.Combos.Add(new());

        if (ImGui.BeginTable("##automation", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("##del");
            ImGui.TableSetupColumn("##num");
            ImGui.TableSetupColumn("Preset / Job Restriction", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();


            for (var i = 0; i < Selected.Combos.Count; i++)
            {

                ImGui.PushID($"Combo{i}");
                var combo = Selected.Combos[i];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    var c = i;
                    new TickScheduler(() => Selected.Combos.RemoveAt(c));
                }

                ImGui.TableNextColumn();

                ImGuiEx.TextV($"#{i + 1}");
                ImGui.TableNextColumn();

                ImGuiEx.SetNextItemFullWidth();
                DrawPresetSelector(combo);
                ImGuiEx.TextV($"Jobs:");
                ImGui.SameLine();
                ImGuiEx.SetNextItemFullWidth();
                ImGuiEx.JobSelector("##job", combo.Jobs, [ImGuiEx.JobSelectorOption.IncludeBase], maxPreviewJobs: 6, noJobSelectedPreview: "Any job");
                ImGui.PopID();
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.TableNextColumn();
            ImGuiEx.TextV("New");
            ImGui.TableNextColumn();
            DrawNewSelector();

            ImGui.EndTable();
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

    static void DrawNewSelector()
    {
        if (ImGui.BeginCombo("##addnew", "Select Preset Here..."))
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
                        if (ImGui.Selectable($"{name}##{x.GUID}"))
                        {
                            Selected.Combos.Add(new()
                            {
                                Preset = x.GUID,
                            });
                        }
                    }
                }
            }
            ImGui.EndCombo();
        }
    }
}
