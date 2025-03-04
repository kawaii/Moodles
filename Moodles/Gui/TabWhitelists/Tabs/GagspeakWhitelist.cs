﻿using Moodles.Data;
using Moodles.OtterGuiHandlers;
using OtterGui.Raii;

namespace Moodles.Gui.TabWhitelists.Tabs;

internal class GagspeakWhitelist : PluginWhitelist
{
    private WhitelistEntryGSpeak Selected => P.OtterGuiHandler.WhitelistGSpeak.Current;

    public override string pluginName { get; } = "GagSpeak";

    protected override void DrawWhitelist()
    {
        P.OtterGuiHandler.WhitelistGSpeak.Draw(200f);
    }

    protected override void DrawHeader()
    {
        if(Selected == null) HeaderDrawer.Draw("GagSpeak Visible Pair Settings", 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
    }

    protected override void Draw()
    {
        // if there are 0 entries in the whitelist, clear the current.
        if(C.WhitelistGSpeak.Count == 0)
        {
            P.OtterGuiHandler.WhitelistGSpeak.EnsureCurrent();
        }

        if(Selected == null)
        {
            using(ImRaii.IEndObject child = ImRaii.Child("##DefaultBox", -Vector2.One, true))
            {
                if(!child) return;
                ImGuiEx.Text($"No GagSpeak Pairs are visible to view the permissions of. Select one to view permissions!");
            }
        }
        else
        {
            HeaderDrawer.Draw("Your Permissions for " + Selected.PlayerName.Censor($"Whitelist entry {C.WhitelistGSpeak.IndexOf(Selected) + 1}"), 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
            using(ImRaii.IEndObject child = ImRaii.Child("##Panel", new(ImGui.GetContentRegionAvail().X - 1f, ImGui.GetContentRegionAvail().Y / 2 - ImGui.GetFrameHeight()), true))
            {
                if(!child) return;

                DrawTableForPermissions(Selected.ClientPermsForPair, "ClientPermsForPair");
            }

            HeaderDrawer.Draw("Permissions " + Selected.PlayerName.Censor($"Whitelist entry {C.WhitelistGSpeak.IndexOf(Selected) + 1}") + " set for You", 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
            using(ImRaii.IEndObject child2 = ImRaii.Child("##Panel2", -Vector2.One, true))
            {
                if(!child2) return;
                DrawTableForPermissions(Selected.PairPermsForClient, "PairPermsForClient");
            }
        }
    }

    private void DrawTableForPermissions(GSpeakPerms perms, string id)
    {
        GSpeakPerms refObject = perms;
        if (ImGui.BeginTable("##wl" + id, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("##txt" + id, ImGuiTableColumnFlags.WidthFixed, 200);
            ImGui.TableSetupColumn("##inp" + id, ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Allowed Status Types:");
            ImGui.TableNextColumn();

            ImGui.BeginDisabled();
            bool previewPos = perms.Pos;
            ImGui.Checkbox("Positive##postive" + id, ref previewPos);
            ImGui.SameLine();
            bool previewNeg = perms.Neg;
            ImGui.Checkbox("Negative##negative" + id, ref previewNeg);
            ImGui.SameLine();
            bool previewSpecial = perms.Special;
            ImGui.Checkbox("Special##special" + id, ref previewSpecial);
            ImGui.EndDisabled();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Maximum Duration:");
            ImGui.TableNextColumn();

            ImGui.BeginDisabled();
            TimeSpan tSpan = TimeSpan.FromTicks(perms.MaxTicks);
            bool perma = perms.Permanent;
            int days = tSpan.Days;
            int hours = tSpan.Hours;
            int minutes = tSpan.Minutes;
            int seconds = tSpan.Seconds;
            Utils.DurationSelector("Any Duration", ref perma, ref days, ref hours, ref minutes, ref seconds);
            ImGui.EndDisabled();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Apply Direction:");
            ImGui.TableNextColumn();

            ImGui.BeginDisabled();
            bool applyOwn = perms.OwnMoodle;
            ImGui.Checkbox("Can Apply Our Moodles##" + id, ref applyOwn);
            bool applyPairs = perms.PairMoodle;
            ImGui.Checkbox("Can Apply Their Moodles##" + id, ref applyPairs);
            ImGui.EndDisabled();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Status Removal:");
            ImGui.TableNextColumn();
            ImGui.BeginDisabled();
            bool remove = perms.Remove;
            ImGui.Checkbox("Can Remove Moodles##" + id, ref remove);
            ImGui.EndDisabled();
            ImGui.EndTable();
        }
    }
}
