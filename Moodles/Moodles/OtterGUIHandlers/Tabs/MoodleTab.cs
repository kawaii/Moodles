using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
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
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.TempWindowing;

namespace Moodles.Moodles.OtterGUIHandlers.Tabs;

internal class MoodleTab
{
    private string Filter = "";

    private IMoodle? Selected => OtterGuiHandler.MoodleFileSystem.Selector?.Selected;

    private readonly OtterGuiHandler    OtterGuiHandler;
    private readonly IMoodlesServices   Services;
    private readonly DalamudServices    DalamudServices;
    private readonly IMoodlesMediator   Mediator;
    private readonly IMoodlesDatabase   Database;
    private readonly IUserList          UserList;

    private readonly StatusSelector StatusSelector;

    public MoodleTab(OtterGuiHandler otterGuiHandler, IMoodlesServices services, DalamudServices dalamudServices, IMoodlesDatabase database, IUserList userList)
    {
        DalamudServices = dalamudServices;
        OtterGuiHandler = otterGuiHandler;
        Services = services;
        Mediator = services.Mediator;
        Database = database;
        UserList = userList;

        StatusSelector = new StatusSelector(Mediator, dalamudServices, services, Database);
    }

    public void Draw()
    {
        OtterGuiHandler.MoodleFileSystem.Selector!.Draw();

        if (Selected == null)
        {
            return;
        }

        ImGui.SameLine();
        using ImRaii.IEndObject group = ImRaii.Group();
        DrawHeader();
        DrawSelected();
    }

    private void DrawHeader()
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

        DrawTargetButton(UserList.LocalPlayer, "Yourself");

        ImGui.SameLine();

        DrawTargetButton(Services.TargetManager.Target, "Target");

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

            string ID = Selected.ID;
            ImGui.InputText($"##id-text", ref ID, 36, ImGuiInputTextFlags.ReadOnly);
            ImGui.TableNextColumn();

            // Title Field
            ImGuiEx.RightFloat("TitleCharLimit", () => ImGuiEx.TextV(ImGuiColors.DalamudGrey2, $"{Selected.Title.Length}/150"), out _, ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X + 5);
            ImGuiEx.TextV($"Title:");
            Formatting();

            Utils.ParseBBSeString(Selected.Title, out string? titleError);
            if (titleError != null)
            {
                ImGuiEx.HelpMarker(titleError, EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }

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
                PluginLog.LogVerbose($"Set Title from UI to: {titleHolder}");
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
                    if (!ImGui.Selectable(Services.Sheets.VFXPaths[i])) continue;
                    
                    Selected.SetVFXPath(Services.Sheets.VFXPaths[i], Mediator);
                    PluginLog.LogVerbose($"Set VFX Path to: {Selected.VFXPath}");
                }

                if (Selected.VFXPath == "Clear")
                {
                    PluginLog.LogVerbose($"Set VFX Path to: {string.Empty}");
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
                        PluginLog.LogVerbose($"Set starting stacks to: {i}");
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.EndDisabled();

            if (Selected.StartingStacks < 1)
            {
                Selected.SetStartingStacks(1, Mediator);
                Selected.SetStackOnReapply(false, Mediator);
                Selected.SetStackIncrementOnReapply(1, Mediator);
                PluginLog.LogVerbose($"Set starting stacks to: {1}");
            }

            if (maxStacks > 1)
            {
                if (Selected.StartingStacks > maxStacks)
                {
                    Selected.SetStartingStacks((int)maxStacks, Mediator);
                    PluginLog.LogVerbose($"Set starting stacks to: {maxStacks}");
                }
            }
            ImGui.TableNextRow();

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
                    PluginLog.LogVerbose($"SetStackOnReapply: {localStacksOnReapply}");
                }
                // if the selected should reapply and we have a stacked moodle.
                if (Selected.StackOnReapply && maxStacks > 1)
                {
                    // display the slider for the stack count.
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(30);

                    int localStacksIncOnReapply = Selected.StackIncrementOnReapply;

                    ImGui.DragInt("Increase By", ref localStacksIncOnReapply, 0.1f, 0, (int)maxStacks);
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        Selected.SetStackIncrementOnReapply(localStacksIncOnReapply, Mediator);
                        PluginLog.LogVerbose($"SetStackIncrementOnReapply: {localStacksIncOnReapply}");
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(30);

                    bool localTimeResetOnStack = Selected.TimeResetsOnStack;
                    if (ImGui.Checkbox("Time Resets On Stack", ref localTimeResetOnStack))
                    {
                        Selected.SetTimeResetsOnStack(localTimeResetOnStack, Mediator);
                        PluginLog.LogVerbose($"Set Time Resets On Stack: {localTimeResetOnStack}");
                    }
                }
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
                PluginLog.LogVerbose($"Set Status Type: {localStatusType}");
            }

            // Duration Field
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Duration:");

            bool noExpire = Selected.Permanent;

            long totalTime = Services.MoodleValidator.GetMoodleDuration(Selected, out int localDays, out int localHours, out int localMinutes, out int localSeconds, out bool offlineCountdown);

            if (totalTime < 1 && !Selected.Permanent)
            {
                ImGuiEx.HelpMarker("Duration must be at least 1 second", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            } 
            
            if (totalTime < PluginConstants.MinSyncMoodleTicks && !Selected.Permanent)
            {
                ImGuiEx.HelpMarker($"Moodles less than {(PluginConstants.MinSyncMoodleTicks / TimeSpan.TicksPerSecond)} seconds will NOT get synchronized.]", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }

            ImGui.TableNextColumn();
            if (DurationSelector("Permanent", ref noExpire, ref localDays, ref localHours, ref localMinutes, ref localSeconds, ref offlineCountdown))
            {
                Selected.SetPermanent(noExpire);
                Selected.SetCountsDownWhenOffline(offlineCountdown);
                Selected.SetDuration(localDays, localHours, localMinutes, localSeconds, Mediator);
                PluginLog.LogVerbose($"Time To: {noExpire} {localDays} {localHours} {localMinutes} {localSeconds} {offlineCountdown}");
            }



            // Dispells on Death Field
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Dispell on Death:");
            ImGuiEx.HelpMarker("Does not apply next Moodle in the sequence.");

            ImGui.TableNextColumn();

            bool localDispellOnDeath = Selected.DispellsOnDeath;

            if (ImGui.Checkbox("Dispell On Death", ref localDispellOnDeath))
            {
                Selected.SetDispellsOnDeath(localDispellOnDeath, Mediator);
                PluginLog.LogVerbose($"SetDispellsOnDeath: {localDispellOnDeath}");
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
                    PluginLog.LogVerbose($"SetDispellable: {localIsDispellable}");
                }
            }

            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Apply on Dispell:");
            ImGuiEx.HelpMarker("The selected Moodle gets applied automatically upon ANY dispell of the current Moodle.");

            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();

            string information = "Apply Moodle On Dispell...";

            IMoodle? dispellableMoodle = Database.GetMoodleNoCreate(Selected.StatusOnDispell);
            if (dispellableMoodle != null)
            {
                information = OtterGuiHandler.MoodleFileSystem.TryGetPathByID(dispellableMoodle.Identifier, out string? path) ? path : dispellableMoodle.ID;
            }

            if (ImGui.BeginCombo("##addnew", information, ImGuiComboFlags.HeightLargest))
            {
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##search", "Filter", ref Filter, 50);

                if (ImGui.Selectable($"Clear", false, ImGuiSelectableFlags.None))
                {
                    Selected.SetStatusOnDispell(Guid.Empty, Mediator);
                    PluginLog.LogVerbose($"SetStatusOnDispell: {Guid.Empty}");
                }
                
                foreach (Moodle moodle in Database.Moodles)
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
                                ImGui.Image(tWrap.Handle, PluginConstants.StatusIconSize * 0.5f);
                                ImGui.SameLine();
                            }

                            if (ImGui.Selectable($"{name}##{moodle.ID}", false, ImGuiSelectableFlags.None))
                            {
                                Selected.SetStatusOnDispell(moodle.Identifier, Mediator);
                                PluginLog.LogVerbose($"SetStatusOnDispell: {moodle.Identifier}");
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

            Utils.ParseBBSeString(Selected.Description, out string? descriptionError);
            if (descriptionError != null)
            {
                ImGuiEx.HelpMarker(descriptionError, EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }

            ImGui.TableNextColumn();
            //ImGuiEx.SetNextItemFullWidth();

            string localDescription = Selected.Description;

            ImGui.InputTextMultiline("##desc", ref localDescription, 500, ImGui.GetContentRegionAvail() - ImGui.GetStyle().CellPadding, ImGuiInputTextFlags.None);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Selected.SetDescription(localDescription, Mediator);
                PluginLog.LogVerbose($"SetDescription: {localDescription}");
            }

            ImGui.EndTable();
        }


        DrawSelectedIcon(statusIconCursorPos);
    }

    private void DrawSelectedIcon(Vector2 statusIconCursorPos)
    {
        if (Selected == null) return;

        IDalamudTextureWrap? tWrap = GetTextureWrapFor(Selected);
        if (tWrap == null) return;

        ImGui.SetCursorPos(statusIconCursorPos);
        ImGui.Image(tWrap.Handle, PluginConstants.StatusIconSize * 2);
    }

    private IDalamudTextureWrap? GetTextureWrapFor(IMoodle moodle)
    {
        if (Selected == null) return null;

        if (Selected.IconID == 0) return null;
        if (!DalamudServices.TextureProvider.TryGetFromGameIcon(Services.MoodleValidator.GetAdjustedIconId((uint)Selected.IconID, (uint)Selected.StartingStacks), out ISharedImmediateTexture? texture)) return null;
        if (!texture.TryGetWrap(out IDalamudTextureWrap? image, out _)) return null;

        return image;
    }

    private void DrawTargetButton(IMoodleHolder? target, string targetName)
    {
        if (Selected == null) return;

        string applyToSelfText      = "No Target Available";
        bool targetNull             = target == null;
        bool hasMoodle              = target?.StatusManager.HasMaxedOutMoodle(Selected, Services.MoodleValidator, out _) ?? false;
        if (!targetNull)
        {
            if (hasMoodle)  applyToSelfText = $"Remove from {targetName}";
            else            applyToSelfText = $"Apply to {targetName}";
        }

        ImGui.BeginDisabled(target == null);
        if (ImGui.Button(applyToSelfText + $"##selfTargetButton{WindowHandler.InternalCounter}"))
        {
            if (hasMoodle)
            {
                target?.StatusManager.RemoveMoodle(Selected, MoodleReasoning.ManualNoFlag, Mediator);
            }
            else
            {
                target?.StatusManager.ApplyMoodle(Selected, MoodleReasoning.ManualFlag, Services.MoodleValidator, UserList, Mediator);
            }
        }

        ImGui.EndDisabled();
    }

    private void Formatting()
    {
        ImGuiEx.HelpMarker($"This field supports formatting tags.\n[color=red]...[/color], [color=5]...[/color] - colored text.\n[glow=blue]...[/glow], [glow=7]...[/glow] - glowing text outline\nThe following colors are available:\n{Enum.GetValues<ECommons.ChatMethods.UIColor>().Select(x => x.ToString()).Where(x => !x.StartsWith("_")).Print()}\nFor extra color, look up numeric value with \"/xldata uicolor\" command\n[i]...[/i] - italic text", ImGuiColors.DalamudWhite, FontAwesomeIcon.Code.ToIconString());
    }

    private bool DurationSelector(string PermanentTitle, ref bool NoExpire, ref int Days, ref int Hours, ref int Minutes, ref int Seconds, ref bool countDownWhenOffline)
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
            modified |= ImGui.IsItemDeactivatedAfterEdit();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("H##h", ref Hours, 0.1f, 0, 23);
            modified |= ImGui.IsItemDeactivatedAfterEdit();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("M##m", ref Minutes, 0.1f, 0, 59);
            modified |= ImGui.IsItemDeactivatedAfterEdit();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("S##s", ref Seconds, 0.1f, 0, 59);
            modified |= ImGui.IsItemDeactivatedAfterEdit();
            ImGui.SameLine();
            if (ImGui.Checkbox("Offline Countdown", ref countDownWhenOffline))
            {
                modified = true;
            }
            ImGuiEx.HelpMarker("When offline this moodle will keep ticking down, and can thus be dispelled when offline.");
        }
        // Wait 5 seconds before firing our status modified event. (helps prevent flooding)
        if (modified) return true;
        // otherwise, return false for the change.
        return false;
    }
}
