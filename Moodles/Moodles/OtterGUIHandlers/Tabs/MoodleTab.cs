using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using ImGuiNET;
using System;
using Dalamud.Interface.Utility.Raii;
using Moodles.Moodles.StatusManaging.Interfaces;
using OtterGui.Filesystem;
using Moodles.Moodles.Services.Interfaces;
using System.Numerics;
using ECommons.ImGuiMethods;
using Moodles.Moodles.Services;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.OtterGUIHandlers.Selectors;
using Moodles.Moodles.Services.Data;
using System.Linq;

namespace Moodles.Moodles.OtterGUIHandlers.Tabs;

internal class MoodleTab
{
    // TEMP
    readonly Vector2 StatusIconSize = new(24, 32);

    string Filter = "";

    IMoodle? Selected => OtterGuiHandler.MoodleFileSystem.Selector?.Selected;

    readonly OtterGuiHandler OtterGuiHandler;
    readonly IMoodlesServices Services;
    readonly DalamudServices DalamudServices;
    readonly IMoodlesMediator Mediator;

    readonly StatusSelector StatusSelector;

    public MoodleTab(OtterGuiHandler otterGuiHandler, IMoodlesServices services, DalamudServices dalamudServices, IMoodlesMediator mediator)
    {
        DalamudServices = dalamudServices;
        OtterGuiHandler = otterGuiHandler;
        Services = services;
        Mediator = mediator;

        StatusSelector = new StatusSelector(Mediator, dalamudServices, services);
    }

    public void Draw()
    {
        if (Selected == null) return;

        OtterGuiHandler.MoodleFileSystem.Selector!.Draw(200f);
        ImGui.SameLine();
        using var group = ImRaii.Group();
        DrawHeader();
        DrawSelected();
    }

    void DrawHeader()
    {
        if (Selected == null) return;

        HeaderDrawer.Draw
        (
            OtterGuiHandler.MoodleFileSystem.FindLeaf
            (
                Selected,
                out FileSystem<IMoodle>.Leaf? leaf
            )
            ? leaf.FullName() : string.Empty,
            0,
            ImGui.GetColorU32(ImGuiCol.FrameBg),
            0,
            HeaderDrawer.Button.IncognitoButton(Services.Configuration.Censor, v => Services.Configuration.Censor = v)
        );
    }

    public void DrawSelected()
    {
        using ImRaii.IEndObject child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || Selected == null) return;

        Vector2 cur = new Vector2(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - StatusIconSize.X * 2, ImGui.GetCursorPosY()) - new Vector2(10, 0);
        if (ImGui.Button("Apply to Yourself"))
        {
            
        }
        ImGui.SameLine();

        var buttonText = DalamudServices.TargetManager.Target is not IPlayerCharacter ? "No Target Selected" : "Apply to target";
        if (ImGui.Button(buttonText))
        {
            
        }

        if (ImGui.BeginTable("##moodles", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 175f);
            ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextColumn();

            // Title Field
            ImGuiEx.RightFloat("TitleCharLimit", () => ImGuiEx.TextV(ImGuiColors.DalamudGrey2, $"{Selected.Title.Length}/150"), out _, ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X + 5);
            ImGuiEx.TextV($"Title:");
            Formatting();

            if (Selected.Title.Length == 0)
            {
                ImGuiEx.HelpMarker("Title can not be empty", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }

            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();

            string titleHolder = Selected.Title;

            ImGui.InputText("##name", ref titleHolder, 150);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Selected.SetTitle(titleHolder, Mediator);
            }

            // Icon Field
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Icon:");
            if (Selected.IconID == 0)
            {
                ImGuiEx.HelpMarker("You must select an icon", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();

            IconInfo? selinfo = Services.MoodlesCache.GetStatusIconInfo((uint)Selected.IconID);

            if (ImGui.BeginCombo("##sel", $"Icon: #{Selected.IconID} {selinfo?.Name}", ImGuiComboFlags.HeightLargest))
            {
                Vector2 cursor = ImGui.GetCursorPos();

                ImGui.Dummy(new Vector2(100, ImGuiHelpers.MainViewport.Size.Y * Services.Configuration.SelectorHeight / 100));
                ImGui.SetCursorPos(cursor);

                StatusSelector.SetSelectedMoodle(Selected);
                StatusSelector.Draw();

                ImGui.EndCombo();
            }

            /*
            // Custom VFX Field
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Custom VFX path:");
            ImGuiEx.HelpMarker("You may select a custom VFX to play upon application.");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            var currentPath = Selected.CustomFXPath;
            if (ImGui.BeginCombo("##vfx", $"VFX: {currentPath}", ImGuiComboFlags.HeightLargest))
            {
                for (var i = 0; i < P.CommonProcessor.StatusEffectPaths.Count; i++)
                {
                    if (ImGui.Selectable(P.CommonProcessor.StatusEffectPaths[i])) Selected.CustomFXPath = P.CommonProcessor.StatusEffectPaths[i];
                }

                if (Selected.CustomFXPath == "Clear")
                {
                    Selected.CustomFXPath = string.Empty;
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextRow();

            // Stack Field
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Stacks:");
            ImGuiEx.HelpMarker("Where the game data contains information about sequential status effect stacks you can select the desired number here. Not all status effects that have stacks follow the same logic due to inconsistencies so the icon you're looking for may be elsewhere.");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            var maxStacks = 1;
            if (P.CommonProcessor.IconStackCounts.TryGetValue((uint)Selected.IconID, out var count))
            {
                maxStacks = (int)count;
            }
            if (maxStacks <= 1) ImGui.BeginDisabled();
            if (ImGui.BeginCombo("##stk", $"{Selected.Stacks}"))
            {
                for (var i = 1; i <= maxStacks; i++)
                {
                    if (ImGui.Selectable($"{i}"))
                    {
                        Selected.Stacks = i;
                        // Inform IPC of change after adjusting stack count.
                        P.IPCProcessor.StatusModified(Selected.GUID);
                    }
                }
                ImGui.EndCombo();
            }
            if (maxStacks <= 1) ImGui.EndDisabled();
            if (Selected.Stacks > maxStacks) Selected.Stacks = maxStacks;
            if (Selected.Stacks < 1)
            {
                Selected.Stacks = 1;
                Selected.StackOnReapply = false;
                Selected.StacksIncOnReapply = 1;
            }
            ImGui.TableNextRow();

            // Description Field
            ImGui.TableNextColumn();
            var cpx = ImGui.GetCursorPosX();
            ImGuiEx.RightFloat("DescCharLimit", () => ImGuiEx.TextV(ImGuiColors.DalamudGrey2, $"{Selected.Description.Length}/500"), out _, ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X);
            ImGuiEx.TextV($"Description:");
            Formatting();
            {
                Utils.ParseBBSeString(Selected.Description, out var error);
                if (error != null)
                {
                    ImGuiEx.HelpMarker(error, EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
            }
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            ImGuiEx.InputTextMultilineExpanding("##desc", ref Selected.Description, 500);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                P.IPCProcessor.StatusModified(Selected.GUID);
            }
            ImGui.TableNextRow();

            // Category Field
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Category:");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            if (ImGuiEx.EnumRadio(ref Selected.Type, true))
            {
                P.IPCProcessor.StatusModified(Selected.GUID);
            }

            // Duration Field
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Duration:");
            if (Selected.TotalDurationSeconds < 1 && !Selected.NoExpire)
            {
                ImGuiEx.HelpMarker("Duration must be at least 1 second", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }
            ImGui.TableNextColumn();
            if (Utils.DurationSelector("Permanent", ref Selected.NoExpire, ref Selected.Days, ref Selected.Hours, ref Selected.Minutes, ref Selected.Seconds))
            {
                P.IPCProcessor.StatusModified(Selected.GUID);
            }

            // Sticky Field
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Sticky:");
            ImGuiEx.HelpMarker("When manually applied outside the scope of an automation preset, this Moodle will not be removed or overridden unless you right-click it off.");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            if (ImGui.Checkbox($"##sticky", ref Selected.AsPermanent))
            {
                P.IPCProcessor.StatusModified(Selected.GUID);
            }

            // Dispelable Field
            if (P.CommonProcessor.DispelableIcons.Contains((uint)Selected.IconID))
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Imply Dispellable:");
                ImGuiEx.HelpMarker("Applies the dispellable indicator to this Moodle implying it can be removed via the use of Esuna. Only available for icons representing negative status effects.");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.Checkbox("##dispel", ref Selected.Dispelable))
                {
                    P.IPCProcessor.StatusModified(Selected.GUID);
                }
            }

            // Stack on Reapply Field
            if (maxStacks > 1)
            {
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Increasing Stacks:");
                ImGuiEx.HelpMarker("Applying a Moodle already active will increase its stack count.\nYou can set how many stacks increase on reapplication.");

                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.Checkbox("##stackonreapply", ref Selected.StackOnReapply))
                {
                    P.IPCProcessor.StatusModified(Selected.GUID);
                }
                // if the selected should reapply and we have a stacked moodle.
                if (Selected.StackOnReapply && maxStacks > 1)
                {
                    // display the slider for the stack count.
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(30);
                    ImGui.DragInt("Increased Stack Count", ref Selected.StacksIncOnReapply, 0.1f, 0, maxStacks);
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        P.IPCProcessor.StatusModified(Selected.GUID);
                    }
                }
            }
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Apply on Dispell:");
            ImGuiEx.HelpMarker("The selected Moodle gets applied automatically upon ANY dispell of the current Moodle.");

            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();

            string information = "Apply Moodle On Dispell...";

            if (C.SavedStatuses.Where(v => v.GUID == Selected.StatusOnDispell).TryGetFirst(out MyStatus myStat))
            {
                information = P.OtterGuiHandler.MoodleFileSystem.TryGetPathByID(myStat.GUID, out var path) ? path : myStat.GUID.ToString();
            }

            if (ImGui.BeginCombo("##addnew", information, ImGuiComboFlags.HeightLargest))
            {
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##search", "Filter", ref Filter, 50);

                if (ImGui.Selectable($"Clear", false, ImGuiSelectableFlags.None))
                {
                    Selected.StatusOnDispell = Guid.Empty;
                    P.IPCProcessor.StatusModified(Selected.GUID);
                }

                foreach (var x in C.SavedStatuses)
                {
                    if (!x.IsValid(out _)) continue;
                    if (Selected.GUID != x.GUID && P.OtterGuiHandler.MoodleFileSystem.TryGetPathByID(x.GUID, out var path))
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
                            if (ImGui.Selectable($"{name}##{x.ID}", false, ImGuiSelectableFlags.None))
                            {
                                Selected.StatusOnDispell = x.GUID;
                                P.IPCProcessor.StatusModified(Selected.GUID);
                            }
                        }
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Applicant:");
            ImGuiEx.HelpMarker("Indicates who applied the Moodle. Changes the colour of the duration counter to be green if the character name and world resolve to yourself.");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputTextWithHint("##applier", "Player Name@World", ref Selected.Applier, 150, C.Censor ? ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                P.IPCProcessor.StatusModified(Selected.GUID);
            }

            ImGui.TableNextColumn();
            ImGuiEx.TextV($"ID:");
            ImGuiEx.HelpMarker("Used in commands to apply moodle.");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputText($"##id-text", Encoding.UTF8.GetBytes(Selected.ID), 36, ImGuiInputTextFlags.ReadOnly);
            */
            ImGui.EndTable();
        }
            
        /*
        if (Selected.IconID != 0 && ThreadLoadImageHandler.TryGetIconTextureWrap(Selected.AdjustedIconID, true, out var image))
        {
            ImGui.SetCursorPos(cur);
            ImGui.Image(image.ImGuiHandle, UI.StatusIconSize * 2);
        }*/
    }

    void Formatting()
    {
        ImGuiEx.HelpMarker("UH OH!");
    }
}
