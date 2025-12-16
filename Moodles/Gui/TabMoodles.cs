using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Moodles.Data;
using Moodles.OtterGuiHandlers;
using OtterGui.Raii;

namespace Moodles.Gui;
public static class TabMoodles
{
    private static bool AsPermanent = false;

    private static MyStatus Selected => P.OtterGuiHandler.MoodleFileSystem.Selector.Selected!;

    private static string Filter = "";
    public static void Draw()
    {
        P.OtterGuiHandler.MoodleFileSystem.Selector.Draw();
        ImGui.SameLine();
        using var group = ImRaii.Group();
        DrawHeader();
        DrawSelected();
    }

    private static void DrawHeader()
    {
        HeaderDrawer.Draw(P.OtterGuiHandler.MoodleFileSystem.FindLeaf(Selected, out var l) ? l.FullName() : "", 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
    }

    public static void DrawSelected()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || Selected == null)
            return;
        {
            var cur = new Vector2(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - UI.StatusIconSize.X * 2, ImGui.GetCursorPosY()) - new Vector2(10, 0);
            if (ImGui.Button("Apply to Yourself"))
            {
                Utils.GetMyStatusManager(Player.NameWithWorld).AddOrUpdate(Selected.PrepareToApply(AsPermanent ? PrepareOptions.Persistent : PrepareOptions.NoOption), UpdateSource.StatusTuple);
            }

            ImGui.SameLine();
            // Determine target state and application intent
            var targetMode = Utils.GetApplyMode();
            var buttonText = targetMode switch
            {
                TargetApplyMode.GSpeakPair => "Apply to Target (via GSpeak)",
                TargetApplyMode.Sundesmo => "Apply to Target (via Sundouleia)",
                TargetApplyMode.Local => "Apply to Target (Locally)",
                _ => "No Target Selected"
            };
            var dis = targetMode is TargetApplyMode.NoTarget;

            if (dis) ImGui.BeginDisabled();
            if (ImGui.Button(buttonText))
            {
                ApplyToTarget(targetMode);
            }
            if (dis) ImGui.EndDisabled();

            if (ImGui.BeginTable("##moodles", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 175f);
                ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextColumn();

                // Title Field
                ImGuiEx.RightFloat("TitleCharLimit", () => ImGuiEx.TextV(ImGuiColors.DalamudGrey2, $"{Selected.Title.Length}/150"), out _, ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X + 5);
                ImGuiEx.TextV($"Title:");
                Formatting();
                {
                    Utils.ParseBBSeString(Selected.Title, out var error);
                    if (error != null)
                    {
                        ImGuiEx.HelpMarker(error, EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                    }
                }
                if (Selected.Title.Length == 0)
                {
                    ImGuiEx.HelpMarker("Title can not be empty", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputText("##name", ref Selected.Title, 150);
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
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
                var selinfo = Utils.GetIconInfo((uint)Selected.IconID);
                if (ImGui.BeginCombo("##sel", $"Icon: #{Selected.IconID} {selinfo?.Name}", ImGuiComboFlags.HeightLargest))
                {
                    var cursor = ImGui.GetCursorPos();
                    ImGui.Dummy(new Vector2(100, ImGuiHelpers.MainViewport.Size.Y * C.SelectorHeight / 100));
                    ImGui.SetCursorPos(cursor);
                    P.StatusSelector.Delegate = Selected;
                    P.StatusSelector.Draw();
                    //P.StatusSelector.Open(Selected);
                    //ImGui.CloseCurrentPopup();
                    ImGui.EndCombo();
                }
                // post update to IPC if a new icon is selected.
                if (Utils.GetIconInfo((uint)Selected.IconID)?.Name != selinfo?.Name)
                {
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                }


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
                            P.IPCProcessor.StatusUpdated(Selected.GUID, false);
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
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                }
                ImGui.TableNextRow();

                // Category Field
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Category:");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                if (ImGuiEx.EnumRadio(ref Selected.Type, true))
                {
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
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
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
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
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                }

                // Dispelable Field
                if (P.CommonProcessor.DispelableIcons.Contains((uint)Selected.IconID))
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Dispelable:");
                    ImGuiEx.HelpMarker("Applies the dispelable indicator to this Moodle. Unless 'Moodles can be Esunad' is enabled in settings, this is only implied.\n" +
                        "Only available for icons representing negative status effects.");
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    if (ImGui.Checkbox("##dispel", ref Selected.Dispelable))
                    {
                        P.IPCProcessor.StatusUpdated(Selected.GUID, false);
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
                        P.IPCProcessor.StatusUpdated(Selected.GUID, false);
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
                            P.IPCProcessor.StatusUpdated(Selected.GUID, false);
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
                        P.IPCProcessor.StatusUpdated(Selected.GUID, false);
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
                                    ImGuiEx.RightFloat($"Selector{x.ID}", () => ImGuiEx.TextV(ImGuiColors.DalamudGrey, directory));
                                }
                                if (ThreadLoadImageHandler.TryGetIconTextureWrap(x.AdjustedIconID, false, out var tex))
                                {
                                    ImGui.Image(tex.Handle, UI.StatusIconSize * 0.5f);
                                    ImGui.SameLine();
                                }
                                if (ImGui.Selectable($"{name}##{x.ID}", false, ImGuiSelectableFlags.None))
                                {
                                    Selected.StatusOnDispell = x.GUID;
                                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                                }
                            }
                        }
                    }
                    ImGui.EndCombo();
                }

                if (Selected.StatusOnDispell != Guid.Empty)
                {
                    ImGui.BeginDisabled(maxStacks <= 1);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Transfer Stacks On Dispell:");
                    ImGui.TableNextColumn();
                    if (ImGui.Checkbox("##TransferStacksOnDispell", ref Selected.TransferStacksOnDispell))
                    {
                        P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                    }
                    ImGui.EndDisabled();
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Applicant:");
                ImGuiEx.HelpMarker("Indicates who applied the Moodle. Changes the colour of the duration counter to be green if the character name and world resolve to yourself.");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##applier", "Player Name@World", ref Selected.Applier, 150, C.Censor ? ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None);
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                }

                if (Selected.Dispelable)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Dispeller:");
                    ImGuiEx.HelpMarker("Indicates who must dispel the moodle, preventing others from dispelling it.\n" +
                        "This only works if the config option allowing others to dispel moodles is enabled.");
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputTextWithHint("##dispeller", "Player Name@World", ref Selected.Dispeller, 150, C.Censor ? ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None);
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                    }
                }

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"ID:");
                ImGuiEx.HelpMarker("Used in commands to apply moodle.");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputText($"##id-text", Encoding.UTF8.GetBytes(Selected.ID), ImGuiInputTextFlags.ReadOnly);

                ImGui.EndTable();
            }

            if (Selected.IconID != 0 && ThreadLoadImageHandler.TryGetIconTextureWrap(Selected.AdjustedIconID, true, out var image))
            {
                ImGui.SetCursorPos(cur);
                ImGui.Image(image.Handle, UI.StatusIconSize * 2);
            }
        }
    }

    public static unsafe void ApplyToTarget(TargetApplyMode mode)
    {
        if (!CharaWatcher.TryGetValue(Svc.Targets.Target?.Address ?? nint.Zero, out Character* chara))
            return;

        try
        {
            switch (mode)
            {
                case TargetApplyMode.GSpeakPair:
                    Selected.SendGSpeakMessage((nint)chara); break;
                case TargetApplyMode.Sundesmo:
                    Selected.SendSundouleiaMessage((nint)chara); break;
                case TargetApplyMode.Local:
                    chara->MyStatusManager().AddOrUpdate(Selected.PrepareToApply(AsPermanent ? PrepareOptions.Persistent : PrepareOptions.NoOption), UpdateSource.StatusTuple); break;
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    public static void Formatting()
    {
        //ImGui.SetWindowFontScale(0.75f);
        ImGuiEx.HelpMarker($"This field supports formatting tags.\n[color=red]...[/color], [color=5]...[/color] - colored text.\n[glow=blue]...[/glow], [glow=7]...[/glow] - glowing text outline\nThe following colors are available:\n{Enum.GetValues<ECommons.ChatMethods.UIColor>().Select(x => x.ToString()).Where(x => !x.StartsWith("_")).Print()}\nFor extra color, look up numeric value with \"/xldata uicolor\" command\n[i]...[/i] - italic text", ImGuiColors.DalamudWhite, FontAwesomeIcon.Code.ToIconString());
        //ImGui.SetWindowFontScale(1f);
    }
}
