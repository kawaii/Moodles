using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using ImGuiNET;
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
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using System.Linq;
using Moodles.Moodles.StatusManaging;
using ECommons;
using System;
using System.Text;

namespace Moodles.Moodles.OtterGUIHandlers.Tabs;

internal class MoodleTab
{
    string Filter = "";

    IMoodle? Selected => OtterGuiHandler.MoodleFileSystem.Selector?.Selected;

    readonly OtterGuiHandler OtterGuiHandler;
    readonly IMoodlesServices Services;
    readonly DalamudServices DalamudServices;
    readonly IMoodlesMediator Mediator;

    readonly StatusSelector StatusSelector;

    public MoodleTab(OtterGuiHandler otterGuiHandler, IMoodlesServices services, DalamudServices dalamudServices)
    {
        DalamudServices = dalamudServices;
        OtterGuiHandler = otterGuiHandler;
        Services = services;
        Mediator = services.Mediator;

        StatusSelector = new StatusSelector(Mediator, dalamudServices, services);
    }

    public void Draw()
    {
        OtterGuiHandler.MoodleFileSystem.Selector!.Draw(200f);

        if (Selected == null) return;

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

        Vector2 statusIconCursorPos = new Vector2(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - PluginConstants.StatusIconSize.X * 2, ImGui.GetCursorPosY()) - new Vector2(10, 0);
        if (ImGui.Button("Apply to Yourself"))
        {
            
        }
        ImGui.SameLine();

        var buttonText = DalamudServices.TargetManager.Target is not IPlayerCharacter ? "No Target Selected" : "Apply to target";
        if (ImGui.Button(buttonText))
        {
            
        }

        if (ImGui.BeginTable("##moodles", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingMask))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 175f);
            ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextColumn();


            // ID field  
            ImGuiEx.TextV($"ID:");
            ImGuiEx.HelpMarker("Used in commands to apply moodle.");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputText($"##id-text", Encoding.UTF8.GetBytes(Selected.ID), 36, ImGuiInputTextFlags.ReadOnly);
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

            
            // Custom VFX Field
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Custom VFX path:");
            ImGuiEx.HelpMarker("You may select a custom VFX to play upon application.");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            string currentPath = Selected.VFXPath;
            if (ImGui.BeginCombo("##vfx", $"VFX: {currentPath}", ImGuiComboFlags.HeightLargest))
            {
                for (var i = 0; i < Services.Sheets.VFXPaths.Count; i++)
                {
                    if (ImGui.Selectable(Services.Sheets.VFXPaths[i])) Selected.SetVFXPath(Services.Sheets.VFXPaths[i], Mediator);
                }

                if (Selected.VFXPath == "Clear")
                {
                    Selected.SetVFXPath(string.Empty, Mediator);
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
            uint maxStacks = 1;
            uint? currentStackCount = Services.Sheets.GetStackCount((uint)Selected.IconID);

            if (currentStackCount != null)
            {
                maxStacks = currentStackCount.Value;
            }
         
            ImGui.BeginDisabled(maxStacks <= 1);

            if (ImGui.BeginCombo("##stk", $"{Selected.StartingStacks}"))
            {
                for (int i = 1; i <= maxStacks; i++)
                {
                    if (ImGui.Selectable($"{i}"))
                    {
                        Selected.SetStartingStacks(i, Mediator);
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.EndDisabled();

            if (Selected.StartingStacks > maxStacks) 
            { 
                Selected.SetStartingStacks((int)maxStacks, Mediator); 
            }

            if (Selected.StartingStacks < 1)
            {
                Selected.SetStartingStacks(1, Mediator);
                Selected.SetStackOnReapply(false, Mediator);
                Selected.SetStackIncrementOnReapply(1, Mediator);
            }
            ImGui.TableNextRow();
            

            // Category Field
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Category:");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();

            StatusType localStatusType = Selected.StatusType;

            if (ImGuiEx.EnumRadio(ref localStatusType, true))
            {
                Selected.SetStatusType(localStatusType, Mediator);
            }

            // Duration Field
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Duration:");

            bool noExpire = Selected.Permanent;

            int totalTime = Services.MoodleValidator.GetMoodleDuration(Selected, out int localDays, out int localHours, out int localMinutes, out int localSeconds);

            if (totalTime < 1 && !Selected.Permanent)
            {
                ImGuiEx.HelpMarker("Duration must be at least 1 second", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }
            ImGui.TableNextColumn();
            if (DurationSelector("Permanent", ref noExpire, ref localDays, ref localHours, ref localMinutes, ref localSeconds))
            {
                Selected.SetPermanent(noExpire);
                Selected.SetDuration(localDays, localHours, localMinutes, localSeconds, Mediator);
            }


            // Dispelable Field
            if (Services.Sheets.StatusIsDispellable((uint)Selected.IconID))
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Imply Dispellable:");
                ImGuiEx.HelpMarker("Applies the dispellable indicator to this Moodle implying it can be removed via the use of Esuna. Only available for icons representing negative status effects.");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();

                bool localIsDispellable = Selected.Dispellable;

                if (ImGui.Checkbox("##dispel", ref localIsDispellable))
                {
                    Selected.SetDispellable(localIsDispellable, Mediator);
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

                bool localStacksOnReapply = Selected.StackOnReapply;

                if (ImGui.Checkbox("##stackonreapply", ref localStacksOnReapply))
                {
                    Selected.SetStackOnReapply(localStacksOnReapply, Mediator);
                }
                // if the selected should reapply and we have a stacked moodle.
                if (Selected.StackOnReapply && maxStacks > 1)
                {
                    // display the slider for the stack count.
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(30);

                    int localStacksIncOnReapply = Selected.StackIncrementOnReapply;

                    ImGui.DragInt("Increased Stack Count", ref localStacksIncOnReapply, 0.1f, 0, (int)maxStacks);
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        Selected.SetStackIncrementOnReapply(localStacksIncOnReapply, Mediator);
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

            if (Services.Configuration.SavedMoodles.Where(v => v.Identifier == Selected.StatusOnDispell).TryGetFirst(out Moodle myStat))
            {
                information = OtterGuiHandler.MoodleFileSystem.TryGetPathByID(myStat.Identifier, out string? path) ? path : myStat.ID;
            }

            if (ImGui.BeginCombo("##addnew", information, ImGuiComboFlags.HeightLargest))
            {
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##search", "Filter", ref Filter, 50);

                if (ImGui.Selectable($"Clear", false, ImGuiSelectableFlags.None))
                {
                    Selected.SetStatusOnDispell(Guid.Empty, Mediator);
                }
                
                foreach (Moodle moodle in Services.Configuration.SavedMoodles)
                {
                    if (!Services.MoodleValidator.IsValid(moodle, out _)) continue;
                    if (Selected.Identifier != moodle.Identifier && OtterGuiHandler.MoodleFileSystem.TryGetPathByID(moodle.Identifier, out string? path))
                    {
                        if (Filter == "" || path.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                        {
                            string[] split = path.Split(@"/");
                            string name = split[^1];
                            string directory = split[0..^1].Join(@"/");

                            if (directory != name)
                            {
                                ImGuiEx.RightFloat($"Selector{moodle.ID}", () => ImGuiEx.Text(ImGuiColors.DalamudGrey, directory));
                            }

                            IDalamudTextureWrap? tWrap = GetTextureWrapFor(Selected);
                            if (tWrap != null)
                            {
                                ImGui.Image(tWrap.ImGuiHandle, PluginConstants.StatusIconSize * 0.5f);
                                ImGui.SameLine();
                            }

                            if (ImGui.Selectable($"{name}##{moodle.ID}", false, ImGuiSelectableFlags.None))
                            {
                                Selected.SetStatusOnDispell(moodle.Identifier, Mediator);
                            }
                        }
                    }
                }
                

                ImGui.EndCombo();
            }


            // Description Field
            ImGui.TableNextRow(ImGuiTableRowFlags.None, ImGui.GetContentRegionAvail().Y);
            ImGui.TableNextColumn();
            ImGuiEx.RightFloat("DescCharLimit", () => ImGuiEx.TextV(ImGuiColors.DalamudGrey2, $"{Selected.Description.Length}/500"), out _, ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X);
            ImGuiEx.TextV($"Description:");
            Formatting();

            ImGui.TableNextColumn();
            //ImGuiEx.SetNextItemFullWidth();

            string localDescription = Selected.Description;

            ImGui.InputTextMultiline("##desc", ref localDescription, 500, ImGui.GetContentRegionAvail() - ImGui.GetStyle().CellPadding, ImGuiInputTextFlags.None);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Selected.SetDescription(localDescription, Mediator);
            }

            ImGui.EndTable();
        }


        DrawSelectedIcon(statusIconCursorPos);
    }

    void DrawSelectedIcon(Vector2 statusIconCursorPos)
    {
        if (Selected == null) return;

        IDalamudTextureWrap? tWrap = GetTextureWrapFor(Selected);
        if (tWrap == null) return;

        ImGui.SetCursorPos(statusIconCursorPos);
        ImGui.Image(tWrap.ImGuiHandle, PluginConstants.StatusIconSize * 2);
    }

    IDalamudTextureWrap? GetTextureWrapFor(IMoodle moodle)
    {
        if (Selected == null) return null;

        if (Selected.IconID == 0) return null;
        if (!DalamudServices.TextureProvider.TryGetFromGameIcon(Services.MoodleValidator.GetAdjustedIconId(Selected), out ISharedImmediateTexture? texture)) return null;
        if (!texture.TryGetWrap(out IDalamudTextureWrap? image, out _)) return null;

        return image;
    }

    void Formatting()
    {
        ImGuiEx.HelpMarker("UH OH!");
    }

    bool DurationSelector(string PermanentTitle, ref bool NoExpire, ref int Days, ref int Hours, ref int Minutes, ref int Seconds)
    {
        var modified = false;

        if (ImGui.Checkbox(PermanentTitle, ref NoExpire))
        {
            modified = true;
        }
        if (!NoExpire)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("D", ref Days, 0.1f, 0, 999);
            if (ImGui.IsItemDeactivatedAfterEdit()) modified = true;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("H##h", ref Hours, 0.1f, 0, 23);
            if (ImGui.IsItemDeactivatedAfterEdit()) modified = true;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("M##m", ref Minutes, 0.1f, 0, 59);
            if (ImGui.IsItemDeactivatedAfterEdit()) modified = true;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("S##s", ref Seconds, 0.1f, 0, 59);
            if (ImGui.IsItemDeactivatedAfterEdit()) modified = true;

        }
        // Wait 5 seconds before firing our status modified event. (helps prevent flooding)
        if (modified) return true;
        // otherwise, return false for the change.
        return false;
    }
}
