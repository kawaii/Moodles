using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using Moodles.Data;
using Moodles.OtterGuiHandlers;
using OtterGui.Raii;

namespace Moodles.Gui;
public static class TabMoodles
{
    static bool AsPermanent = false;
    static MyStatus Selected => P.OtterGuiHandler.MoodleFileSystem.Selector.Selected;
    static string Filter = "";
    public static void Draw()
    {
        P.OtterGuiHandler.MoodleFileSystem.Selector.Draw(200f);
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
                Utils.GetMyStatusManager(Player.NameWithWorld).AddOrUpdate(Selected.PrepareToApply(AsPermanent ? PrepareOptions.Persistent : PrepareOptions.NoOption));
            }
            ImGui.SameLine();

            var dis = Svc.Targets.Target is not PlayerCharacter;
            if (dis) ImGui.BeginDisabled();
            var isMare = Utils.GetMarePlayers().Contains(Svc.Targets.Target?.Address ?? -1);
            if (ImGui.Button($"Apply to Target ({(isMare?"via Mare Synchronos":"Locally")})"))
            {
                try
                {
                    var target = (PlayerCharacter)Svc.Targets.Target;
                    if (!isMare)
                    {
                        Utils.GetMyStatusManager(target.GetNameWithWorld()).AddOrUpdate(Selected.PrepareToApply(AsPermanent ? PrepareOptions.Persistent : PrepareOptions.NoOption));
                    }
                    else
                    {
                        Selected.SendMareMessage(target);
                    }
                }
                catch(Exception e)
                {
                    e.Log();
                }
            }
            if (isMare) { ImGuiEx.HelpMarker("This doesn't do anything yet, why are you clicking it? :)", color: ImGuiColors.DalamudRed); }
            if (dis) ImGui.EndDisabled();

            if (ImGui.BeginTable("##moodles", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 175f);
                ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
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
                if(Selected.Title.Length == 0)
                {
                    ImGuiEx.HelpMarker("Title can not be empty", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputText("##name", ref Selected.Title, 150);
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
                ImGui.TableNextRow(); 
                
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
                if(maxStacks <= 1) ImGui.BeginDisabled();
                if (ImGui.BeginCombo("##stk", $"{Selected.Stacks}"))
                {
                    for (int i = 1; i <= maxStacks; i++)
                    {
                        if (ImGui.Selectable($"{i}")) Selected.Stacks = i;
                    }
                    ImGui.EndCombo();
                }
                if (maxStacks <= 1) ImGui.EndDisabled();
                if (Selected.Stacks > maxStacks) Selected.Stacks = maxStacks;
                if (Selected.Stacks < 1) Selected.Stacks = 1;
                ImGui.TableNextRow();

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
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Applicant:");
                ImGuiEx.HelpMarker("Indicates who applied the Moodle. Changes the colour of the duration counter to be green if the character name and world resolve to yourself.");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##applier", "Player Name@World", ref Selected.Applier, 150, C.Censor ? ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None);

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Category:");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGuiEx.EnumRadio(ref Selected.Type, true);

                if (P.CommonProcessor.DispelableIcons.Contains((uint)Selected.IconID))
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Dispellable:");
                    ImGuiEx.HelpMarker("Applies the dispellable indicator to this Moodle implying it can be removed via the use of Esuna. Only available for icons representing negative status effects.");
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.Checkbox("##dispel", ref Selected.Dispelable);
                }

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Duration:");
                if(Selected.TotalDurationSeconds < 1 && !Selected.NoExpire)
                {
                    ImGuiEx.HelpMarker("Duration must be at least 1 second", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
                ImGui.TableNextColumn();

                Utils.DurationSelector("Permanent", ref Selected.NoExpire, ref Selected.Days, ref Selected.Hours, ref Selected.Minutes, ref Selected.Seconds);

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Sticky:");
                ImGuiEx.HelpMarker("When manually applied outside the scope of an automation preset, this Moodle will not be removed or overridden unless you right-click it off.");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.Checkbox($"##sticky", ref Selected.AsPermanent);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"ID:");
                ImGuiEx.HelpMarker("Used in commands to apply moodle.");
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputText($"##id-text", Encoding.UTF8.GetBytes(Selected.ID), 36, ImGuiInputTextFlags.ReadOnly);

                ImGui.EndTable();
            }

            if (Selected.IconID != 0 && ThreadLoadImageHandler.TryGetIconTextureWrap(Selected.AdjustedIconID, true, out var image))
            {
                ImGui.SetCursorPos(cur);
                ImGui.Image(image.ImGuiHandle, UI.StatusIconSize * 2);
            }
        }
    }
    public static void Formatting()
    {
        //ImGui.SetWindowFontScale(0.75f);
        ImGuiEx.HelpMarker($"This field supports formatting tags.\n[color=red]...[/color], [color=5]...[/color] - colored text.\n[glow=blue]...[/glow], [glow=7]...[/glow] - glowing text outline\nThe following colors are available:\n{Enum.GetValues<ECommons.ChatMethods.UIColor>().Select(x => x.ToString()).Where(x => !x.StartsWith("_")).Print()}\nFor extra color, look up numeric value with \"/xldata uicolor\" command\n[i]...[/i] - italic text", ImGuiColors.DalamudWhite, FontAwesomeIcon.Code.ToIconString());
        //ImGui.SetWindowFontScale(1f);
    }
}
