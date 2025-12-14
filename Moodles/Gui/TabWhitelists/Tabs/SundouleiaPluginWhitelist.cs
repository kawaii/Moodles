using Moodles.Data;
using Moodles.OtterGuiHandlers;
using OtterGui.Raii;

namespace Moodles.Gui.TabWhitelists.Tabs;

internal class SundouleiaPluginWhitelist : PluginWhitelist
{
    private WhitelistEntrySundouleia Selected => P.OtterGuiHandler.WhitelistSundouleia.Current!;

    public override string pluginName { get; } = "Sundouleia";

    protected override void DrawWhitelist()
    {
        P.OtterGuiHandler.WhitelistSundouleia.Draw(200f);
    }

    protected override void DrawHeader()
    {
        if(Selected is null) HeaderDrawer.Draw("Sundouleia Whitelist", 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
    }

    protected override void Draw()
    {
        // if there are 0 entries in the whitelist, clear the current.
        if(C.WhitelistSundouleia.Count == 0)
        {
            P.OtterGuiHandler.WhitelistSundouleia.EnsureCurrent();
        }

        if(Selected is null)
        {
            using(var child = ImRaii.Child("##DefaultBox", -Vector2.One, true))
            {
                if(!child) return;
                ImGuiEx.Text($"Select a rendered pair to view permissions.");
            }
        }
        else
        {
            var selectedDispName = Selected.PlayerName.Censor($"Whitelist entry {C.WhitelistSundouleia.IndexOf(Selected) + 1}");
            HeaderDrawer.Draw($"Your Permissions for {selectedDispName}", 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
            using(var child = ImRaii.Child("##Panel", new(ImGui.GetContentRegionAvail().X - 1f, ImGui.GetContentRegionAvail().Y / 2 - ImGui.GetFrameHeight()), true))
            {
                if(!child) return;
                DrawTableForPermissions(Selected.ClientAccess, Selected.ClientMaxTime, "AccessPermissionsForPair");
            }

            HeaderDrawer.Draw($"{selectedDispName}'s Permissions for You", 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0, HeaderDrawer.Button.IncognitoButton(C.Censor, v => C.Censor = v));
            using(var child2 = ImRaii.Child("##Panel2", -Vector2.One, true))
            {
                if(!child2) return;
                DrawTableForPermissions(Selected.Access, Selected.MaxTime, "PairAccessPermsForClient");
            }
        }
    }

    private void DrawTableForPermissions(MoodleAccess access, TimeSpan maxTime, string id)
    {
        if(ImGui.BeginTable("##wl" + id, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("##txt" + id, ImGuiTableColumnFlags.WidthFixed, 200);
            ImGui.TableSetupColumn("##inp" + id, ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Allowed Status Types:");
            ImGui.TableNextColumn();

            ImGui.BeginDisabled();
            StaticCheckbox("Positive##positive" + id, access.HasAny(MoodleAccess.Positive));
            ImGui.SameLine();
            StaticCheckbox("Negative##negative" + id, access.HasAny(MoodleAccess.Negative));
            ImGui.SameLine();
            StaticCheckbox("Special##special" + id, access.HasAny(MoodleAccess.Special));
            ImGui.EndDisabled();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Maximum Duration:");
            ImGui.TableNextColumn();

            ImGui.BeginDisabled();
            var (days, hours, minutes, seconds) = (maxTime.Days, maxTime.Hours, maxTime.Minutes, maxTime.Seconds);
            var permanent = access.HasAny(MoodleAccess.Permanent);
            Utils.DurationSelector("Any Duration", ref permanent, ref days, ref hours, ref minutes, ref seconds);
            ImGui.EndDisabled();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Apply Direction:");
            ImGui.TableNextColumn();

            ImGui.BeginDisabled();
            StaticCheckbox("Can Apply Client Moodles##" + id, access.HasAny(MoodleAccess.AllowOwn));
            ImGui.SameLine();
            StaticCheckbox("Can Apply Pairs Moodles##" + id, access.HasAny(MoodleAccess.AllowOther));
            ImGui.EndDisabled();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV($"Status Removal:");
            ImGui.TableNextColumn();
            ImGui.BeginDisabled();
            StaticCheckbox("Can Remove Moodles You Applied##" + id, access.HasAny(MoodleAccess.RemoveApplied));
            ImGui.SameLine();
            StaticCheckbox("Can Remove Any Moodles##" + id, access.HasAny(MoodleAccess.RemoveAny));
            ImGui.EndDisabled();
            ImGui.EndTable();
        }
    }

    public void StaticCheckbox(string label, bool value)
    {
        var tmpVal = value;
        ImGui.Checkbox(label, ref tmpVal);
    }
}
