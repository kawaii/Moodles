using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Moodles.Data;
using Moodles.OtterGuiHandlers;
using OtterGui.Raii;
using OtterGui.Text;
using System.Net;
using static FFXIVClientStructs.FFXIV.Client.Game.StatusManager.Delegates;
using static System.Net.Mime.MediaTypeNames;

namespace Moodles.Gui;
public static class TabMoodles
{
    private static bool AsPermanent = false;

    private static MyStatus Selected => P.OtterGuiHandler.MoodleFileSystem.Selector.Selected!;

    private static string Filter = "";
    public static void Draw()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
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

        ImGui.Text($"Hovering Over: {P.CommonProcessor.HoveringOver:X}");
        
        var cur = new Vector2(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - UI.StatusIconSize.X * 2, ImGui.GetCursorPosY()) - new Vector2(10, 0);
        if (ImGui.Button("Apply to Yourself"))
        {
            Utils.GetMyStatusManager(LocalPlayer.NameWithWorld).AddOrUpdate(Selected.PrepareToApply(AsPermanent ? PrepareOptions.Persistent : PrepareOptions.NoOption), UpdateSource.StatusTuple);
        }
#if DEBUG
        ImGui.SameLine();
        if (ImGui.Button("Apply to Yourself (As Locked)"))
        {
            Utils.GetMyStatusManager(LocalPlayer.NameWithWorld).AddOrUpdateLocked(Selected.PrepareToApply(AsPermanent ? PrepareOptions.Persistent : PrepareOptions.NoOption));
        }
#endif

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
        // Permissions are validated via internal logic behavior.
        var dis = targetMode is TargetApplyMode.NoTarget;

        if (dis) ImGui.BeginDisabled();
        if (ImGui.Button(buttonText))
        {
            ApplyToTarget(targetMode);
        }
        if (dis) ImGui.EndDisabled();

        // Store maxStacks before drawing further.
        var maxStacks = P.CommonProcessor.IconStackCounts.TryGetValue((uint)Selected.IconID, out var count) ? (int)count : 1;

        DrawMoodleEssentials();
        DrawChaining();
        DrawStacking(maxStacks);
        DrawDispelling();

        if (Selected.IconID != 0 && ThreadLoadImageHandler.TryGetIconTextureWrap(Selected.AdjustedIconID, true, out var image))
        {
            ImGui.SetCursorPos(cur);
            ImGui.Image(image.Handle, UI.StatusIconSize * 2);
        }
    }

    private static void DrawMoodleEssentials()
    {
        if (ImGui.BeginTable("##essentials", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 175f);
            ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthStretch);

            // Essentials
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"ID:");
            ImGuiEx.HelpMarker("Used in commands to apply moodle.");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled)))
                ImGui.InputText($"##id-text", Encoding.UTF8.GetBytes(Selected.ID), ImGuiInputTextFlags.ReadOnly);
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
                ImGui.EndCombo();
            }
            // post update to IPC if a new icon is selected.
            if (Utils.GetIconInfo((uint)Selected.IconID)?.Name != selinfo?.Name)
            {
                CleanupSelected();
                P.IPCProcessor.StatusUpdated(Selected.GUID, false);
            }
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
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Selected.CustomFXPath = string.Empty;
            }

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGuiEx.RightFloat("TitleCharLimit", () => ImGuiEx.TextV(ImGuiColors.DalamudGrey2, $"{Selected.Title.Length}/150"), out _, ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X);
            ImGuiEx.TextV($"Title:");
            Formatting();
            Utils.ParseBBSeString(Selected.Title, out var titleErr);
            if (titleErr != null)
            {
                ImGuiEx.HelpMarker(titleErr, EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
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
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var cpx = ImGui.GetCursorPosX();
            ImGuiEx.RightFloat("DescCharLimit", () => ImGuiEx.TextV(ImGuiColors.DalamudGrey2, $"{Selected.Description.Length}/500"), out _, ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X);
            ImGuiEx.TextV($"Description:");
            Formatting();
            Utils.ParseBBSeString(Selected.Description, out var descErr);
            if (descErr != null)
            {
                ImGuiEx.HelpMarker(descErr, EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            ImGuiEx.InputTextMultilineExpanding("##desc", ref Selected.Description, 500);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                P.IPCProcessor.StatusUpdated(Selected.GUID, false);
            }

            // Category
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Category:");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            if (ImGuiEx.EnumRadio(ref Selected.Type, true))
            {
                P.IPCProcessor.StatusUpdated(Selected.GUID, false);
            }
            ImGui.TableNextRow();

            // Duration
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
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGuiEx.TextV("Status Behavior:");
            ImGui.TableNextColumn();
            var persistTime = Selected.Modifiers.Has(Modifiers.PersistExpireTime);
            if (ImGui.Checkbox("Persist Expire Time##noOverlapTime", ref persistTime))
            {
                Selected.Modifiers.Set(Modifiers.PersistExpireTime, persistTime);
                P.IPCProcessor.StatusUpdated(Selected.GUID, false);
            }
            ImGuiEx.Tooltip("When enabled, any reapplication of this moodle keeps it's expire time.");

            ImGui.SameLine();
            if (ImGui.Checkbox($"Sticky##sticky", ref Selected.AsPermanent))
            {
                P.IPCProcessor.StatusUpdated(Selected.GUID, false);
            }
            ImGuiEx.Tooltip("When manually applied outside the scope of an automation preset, this Moodle will not be removed or overridden unless you right-click it off.");


            ImGui.EndTable();
        }
    }

    // Stacking based paramaters.
    private static void DrawStacking(int maxStacks)
    {
        if (maxStacks <= 1)
            return;

        if (ImGui.BeginTable("##stacking", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 175f);
            ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Initial Stacks:");
            ImGuiEx.HelpMarker("The number of stacks initially applied with the moodle.");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            if (ImGui.BeginCombo("##stk", StackText(Selected.Stacks)))
            {
                for (var i = 1; i <= maxStacks; i++)
                {
                    if (ImGui.Selectable(StackText(i), Selected.Stacks == i))
                    {
                        Selected.Stacks = i;
                        P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.TableNextRow();

            // Would rather put these in the same row...
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Stack Steps:");
            ImGuiEx.HelpMarker("If the same moodle is reapplied, the applied stacks increment by this many stacks.");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
            if (ImGui.BeginCombo("##incStk", StackText(Selected.StackSteps)))
            {
                for (var i = 0; i <= maxStacks; i++)
                {
                    if (ImGui.Selectable(StackText(i), Selected.StackSteps == i))
                    {
                        Selected.StackSteps = i;
                        // Update modifiers.
                        Selected.Modifiers = (Selected.StackSteps > 0) ? Selected.Modifiers | Modifiers.StacksIncrease : Selected.Modifiers & ~Modifiers.StacksIncrease;
                        P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            var stacksRoll = Selected.Modifiers.Has(Modifiers.StacksRollOver);
            if (ImGui.Checkbox("Roll Over Stacks##stkroll", ref stacksRoll))
            {
                Selected.Modifiers.Set(Modifiers.StacksRollOver, stacksRoll);
                P.IPCProcessor.StatusUpdated(Selected.GUID, false);
            }
            ImGuiEx.Tooltip("When a stack reaches its cap, it starts over and counts up again.");

            if (Selected.ChainedStatus != Guid.Empty)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Chained Status Behavior:");
                ImGuiEx.HelpMarker("How stacks from this moodle carry to the chained status.");
                ImGui.TableNextColumn();
                var moveStacks = Selected.Modifiers.Has(Modifiers.StacksMoveToChain);
                if (ImGui.Checkbox("Transfer Stacks", ref moveStacks))
                {
                    Selected.Modifiers.Set(Modifiers.StacksMoveToChain, moveStacks);
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                }
                ImGui.SameLine();
                var carryStacks = Selected.Modifiers.Has(Modifiers.StacksCarryToChain);

                if (ImGui.Checkbox("Carry Over Stacks", ref carryStacks))
                {
                    Selected.Modifiers.Set(Modifiers.StacksCarryToChain, carryStacks);
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                }
                ImGuiEx.Tooltip("When the reapplication increase exceeds the max stacks, the remainder is added to the chained status.");

                ImGui.SameLine();
                var persist = Selected.Modifiers.Has(Modifiers.PersistAfterTrigger);
                if (ImGui.Checkbox("Persist", ref persist))
                {
                    Selected.Modifiers.Set(Modifiers.PersistAfterTrigger, persist);
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                }
                ImGuiEx.Tooltip("Keeps this moodle after chain is triggered.");
            }
            ImGui.EndTable();
        }

        string StackText(int v) => v == 0 ? "No Stack Increase" : $"{v} {(v == 1 ? "Stack" : "Stacks")}";
    }

    private static void DrawDispelling()
    {
        if (!P.CommonProcessor.DispelableIcons.Contains((uint)Selected.IconID))
            return;

        if (ImGui.BeginTable("##dispelling", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 175f);
            ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Dispelable:");
            ImGuiEx.HelpMarker("Makes the moodle dispelable. This is only visual unless 'Moodles can be Esunad' is enabled in settings.");
            ImGui.TableNextColumn();
            var canDispel = Selected.Modifiers.Has(Modifiers.CanDispel);
            if (ImGui.Checkbox("##dispel", ref canDispel))
            {
                Selected.Modifiers.Set(Modifiers.CanDispel, canDispel);
                P.IPCProcessor.StatusUpdated(Selected.GUID, false);
            }

            if (canDispel)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Dispeller:");
                ImGuiEx.HelpMarker("An optional field to spesify who the moodle must be dispelled by, preventing others from doing so");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("Dispeller##dispeller", "Player Name@World", ref Selected.Dispeller, 150, C.Censor ? ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None);
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                }
            }

            ImGui.EndTable();
        }
    }

    private static void DrawChaining()
    {
        if (ImGui.BeginTable("##chaining", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 175f);
            ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextColumn();
            ImGuiEx.TextV("Chained Status:");
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            string curChainPath = "Moodle to Chain... (Optional)";
            if (C.SavedStatuses.Where(v => v.GUID == Selected.ChainedStatus).TryGetFirst(out MyStatus myStat))
            {
                curChainPath = P.OtterGuiHandler.MoodleFileSystem.TryGetPathByID(myStat.GUID, out var path) ? path : myStat.GUID.ToString();
            }

            if (ImGui.BeginCombo("##chainedStatus", curChainPath, ImGuiComboFlags.HeightLargest))
            {
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##search", "Filter", ref Filter, 50);

                if (ImGui.Selectable($"Clear", false, ImGuiSelectableFlags.None))
                {
                    Selected.ChainedStatus = Guid.Empty;
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
                                Selected.ChainedStatus = x.GUID;
                                P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                            }
                        }
                    }
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Selected.ChainedStatus = Guid.Empty;
                P.IPCProcessor.StatusUpdated(Selected.GUID, false);
            }

            if (Selected.ChainedStatus != Guid.Empty)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV("Chain Trigger:");
                ImGui.TableNextColumn();
                if (ImGuiEx.EnumRadio(ref Selected.ChainTrigger, true))
                {
                    P.IPCProcessor.StatusUpdated(Selected.GUID, false);
                }
            }

            ImGui.EndTable();
        }
    }

    // Whenever a new icon is selected, new moodle properties are defined entirely,
    // and the rest of the data should be updated.
    private static void CleanupSelected()
    {
        var maxStacks = P.CommonProcessor.IconStackCounts.TryGetValue((uint)Selected.IconID, out var count) ? (int)count : 1;
        Selected.Stacks = Math.Min(Selected.Stacks, maxStacks);
        Selected.StackSteps = Math.Min(Selected.StackSteps, maxStacks);
        // Ensure modifiers are correct.
        Selected.Modifiers = (Selected.StackSteps > 0) 
            ? Selected.Modifiers | Modifiers.StacksIncrease : Selected.Modifiers & ~Modifiers.StacksIncrease;
        // Clear dispeller if not dispellable.
        if (!P.CommonProcessor.DispelableIcons.Contains((uint)Selected.IconID))
        {
            Selected.Modifiers &= ~Modifiers.CanDispel;
            Selected.Dispeller = "";
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
        ImGuiEx.HelpMarker($"This field supports formatting tags.\n[color=red]...[/color], [color=5]...[/color] - colored text.\n[glow=blue]...[/glow], [glow=7]...[/glow] - glowing text outline\nThe following colors are available:\n{Enum.GetValues<ECommons.ChatMethods.UIColor>().Select(x => x.ToString()).Where(x => !x.StartsWith("_")).Print()}\nFor extra color, look up numeric value with \"/xldata uicolor\" command\n[i]...[/i] - italic text", ImGuiColors.DalamudWhite, FontAwesomeIcon.Code.ToIconString());
    }
}
