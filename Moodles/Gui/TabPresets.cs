using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using Moodles.Data;
using Moodles.OtterGuiHandlers;
using OtterGui.Raii;

namespace Moodles.Gui;
public static class TabPresets
{
    static bool IsMoodleSelection = false;
    private static Dictionary<PresetApplicationType, string> ApplicationTypes = new()
    {
        [PresetApplicationType.ReplaceAll] = "Replace all current statuses",
        [PresetApplicationType.UpdateExisting] = "Update duration of existing",
        [PresetApplicationType.IgnoreExisting] = "Ignore existing",
    };
    static string Filter = "";

    static Preset Selected => P.OtterGuiHandler.PresetFileSystem.Selector.Selected;
    public static void Draw()
    {
        if (IsMoodleSelection)
        {
            P.OtterGuiHandler.MoodleFileSystem.Selector.Draw(200f);
        }
        else
        {
            P.OtterGuiHandler.PresetFileSystem.Selector.Draw(200f);
        }
        ImGui.SameLine();
        using var group = ImRaii.Group();
        DrawHeader();
        DrawSelected();
    }

    private static void DrawHeader()
    {
        HeaderDrawer.Draw(P.OtterGuiHandler.PresetFileSystem.FindLeaf(Selected, out var l)?l.FullName():"", 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
    }

    public static void DrawSelected()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || Selected == null)
            return;
        {
            if (ImGui.Button("Apply to Yourself"))
            {
                Utils.GetMyStatusManager(Player.NameWithWorld).ApplyPreset(Selected);
            }
            ImGui.SameLine();
            if (ImGui.Button("Apply to Target") && Svc.Targets.Target is PlayerCharacter pc)
            {
                Utils.GetMyStatusManager(pc.GetNameWithWorld()).ApplyPreset(Selected);
            }

            ImGuiEx.TextV("On application:");
            ImGui.SameLine();
            ImGuiEx.SetNextItemFullWidth();
            ImGuiEx.EnumCombo("##on", ref Selected.ApplicationType, ApplicationTypes);
            ImGuiEx.SetNextItemFullWidth();
            if(ImGui.BeginCombo("##addnew", "Add new Moodle..."))
            {
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##search", "Filter", ref Filter, 50);
                foreach (var x in C.SavedStatuses)
                {
                    if (!Selected.Statuses.Contains(x.GUID) && P.OtterGuiHandler.MoodleFileSystem.TryGetPathByID(x.GUID, out var path))
                    {
                        if (Filter == "" || path.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                        {
                            var split = path.Split(@"/");
                            var name = split[^1];
                            var directory = split[0..^1].Join(@"/");
                            if (directory != name)
                            {
                                ImGuiEx.RightFloat($"Selector{x.ID}", () => ImGuiEx.Text(ImGuiColors.DalamudGrey, directory));
                            }
                            if (ThreadLoadImageHandler.TryGetIconTextureWrap(x.AdjustedIconID, false, out var tex))
                            {
                                ImGui.Image(tex.ImGuiHandle, UI.StatusIconSize * 0.5f);
                                ImGui.SameLine();
                            }
                            if (ImGui.Selectable($"{name}##{x.ID}", false, ImGuiSelectableFlags.DontClosePopups))
                            {
                                Selected.Statuses.Add(x.GUID);
                            }
                        }
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Separator();

            if (ImGui.BeginTable("##presets", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Controls");
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(" ");


                for (var i = 0;i< Selected.Statuses.Count;i++) 
                {
                    var statusId = Selected.Statuses[i];
                    var statusPath = P.OtterGuiHandler.MoodleFileSystem.TryGetPathByID(statusId, out var path) ? path : statusId.ToString();
                    var status = C.SavedStatuses.FirstOrDefault(x => x.GUID == statusId);
                    if(status != null)
                    {
                        ImGui.PushID(status.ID);
                        if(i>0)ImGui.TableNextRow();
                        ImGui.TableNextColumn();

                        if (ImGui.ArrowButton("up", ImGuiDir.Up) && i > 0)
                        {
                            (Selected.Statuses[i - 1], Selected.Statuses[i]) = (Selected.Statuses[i], Selected.Statuses[i - 1]);
                        }
                        ImGui.SameLine();
                        if (ImGui.ArrowButton("down", ImGuiDir.Down) && i < Selected.Statuses.Count - 1)
                        {
                            (Selected.Statuses[i + 1], Selected.Statuses[i]) = (Selected.Statuses[i], Selected.Statuses[i + 1]);
                        }

                        ImGui.TableNextColumn();

                        if(ThreadLoadImageHandler.TryGetIconTextureWrap(status.AdjustedIconID, false, out var tex))
                        {
                            ImGui.Image(tex.ImGuiHandle, UI.StatusIconSize * 0.75f);
                            ImGui.SameLine();
                        }
                        ImGuiEx.TextV($"{statusPath}");
                        ImGuiEx.Tooltip($"{status.Title}\n\n{status.Description}");

                        ImGui.TableNextColumn();

                        if (ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                        {
                            new TickScheduler(() => Selected.Statuses.Remove(statusId));
                        }

                        ImGui.PopID();
                    }
                    else
                    {
                        new TickScheduler(() => Selected.Statuses.Remove(statusId));
                    }
                }

                ImGui.EndTable();
            }
        }
    }
}
