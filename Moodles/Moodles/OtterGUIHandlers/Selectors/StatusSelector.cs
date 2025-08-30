using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using Moodles.Moodles.TempWindowing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moodles.Moodles.OtterGUIHandlers.Selectors;

internal class StatusSelector : Window
{
    IMoodle? selectedMoodle;

    SortOption lastItemSortOption = SortOption.Numerical;
    bool? IsFCStatus = null;
    bool? IsStackable = null;
    string Filter = "";

    bool Fullscreen = false;

    readonly IconInfo[] CachedAvailableIcons;

    IEnumerable<IconInfo> AvailableIcons = [];
    IEnumerable<IconInfo> AvailableIconsFav = [];
    IEnumerable<IconInfo> AvailableIconsPos = [];
    IEnumerable<IconInfo> AvailableIconsNeg = [];
    IEnumerable<IconInfo> AvailableIconsSpec = [];

    bool Valid => selectedMoodle != null && Database.GetMoodleNoCreate(selectedMoodle.Identifier) != null;

    readonly IMoodlesMediator Mediator;
    readonly DalamudServices DalamudServices;
    readonly IMoodlesServices Services;
    readonly ClassJobSelector ClassJobSelector;
    readonly IMoodlesDatabase Database;

    public StatusSelector(IMoodlesMediator mediator, DalamudServices dalamudServices, IMoodlesServices services, IMoodlesDatabase database) : base("Select Icon")
    {
        Mediator = mediator;
        DalamudServices = dalamudServices;
        Services = services;
        ClassJobSelector = new ClassJobSelector(services, dalamudServices);
        Database = database;

        CachedAvailableIcons = Services.Sheets.IconIDs.Select(iconId => Services.MoodlesCache.GetStatusIconInfo(iconId, false)).Where(x => x.HasValue).Cast<IconInfo>().ToArray();
        RebuildChaches();
    }

    public void SetSelectedMoodle(IMoodle? moodle)
    {
        selectedMoodle = moodle;
    }

    public override void Draw()
    {
        if (!Valid)
        {
            ImGuiEx.Text(EColor.RedBright, "Edited status no longer seems to exist.");
        }

        ImGui.SetNextItemWidth(150f);
        if (ImGui.InputTextWithHint($"##search{WindowHandler.InternalCounter}", "Filter...", ref Filter, 50))
        {
            RebuildChaches();
        }

        ImGui.SameLine();

        if (ImGui.Checkbox($"Prefill Data##{WindowHandler.InternalCounter}", ref Services.Configuration.AutoFill))
        {
            RebuildChaches();
        }

        ImGuiEx.HelpMarker("Prefills the Title and Description inputs with data from the game itself regarding the icon. Requires those fields to be empty or unchanged from previous prefill data.");

        ImGui.SameLine();

        if (ImGuiEx.Checkbox($"Stackable##{WindowHandler.InternalCounter}", ref IsStackable))
        {
            RebuildChaches();
        }

        ImGuiEx.HelpMarker("Toggles the filter between all status effecs, those with stacks only, and those without any stacks at all.");

        ImGui.SameLine();

        if (ClassJobSelector.Draw())
        {
            RebuildChaches();
        }

        ImGui.SameLine();

        ImGuiEx.Text("Sorting:");

        ImGui.SameLine();

        ImGui.SetNextItemWidth(100f);

        ImGuiEx.EnumCombo($"##order{WindowHandler.InternalCounter}", ref Services.Configuration.IconSortOption);

        if (lastItemSortOption != Services.Configuration.IconSortOption)
        {
            lastItemSortOption = Services.Configuration.IconSortOption;
            RebuildChaches();
        }

        if (!ImGui.BeginChild($"child##{WindowHandler.InternalCounter}")) return;

        if (Services.Configuration.FavIcons.Count > 0)
        {
            IconTable($"Favourites##{WindowHandler.InternalCounter}", AvailableIconsFav);
        }

        IconTable($"Positive Status Effects##{WindowHandler.InternalCounter}", AvailableIconsPos);
        IconTable($"Negative Status Effects##{WindowHandler.InternalCounter}", AvailableIconsNeg);
        IconTable($"Special Status Effects##{WindowHandler.InternalCounter}", AvailableIconsSpec);

        ImGui.EndChild();
    }

    void RebuildChaches()
    {
        PluginLog.LogVerbose("Rebuild Icon Search Cache");

        AvailableIcons = CachedAvailableIcons
            .Where(x => Filter == string.Empty || x.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase) || x.IconID.ToString().Contains(Filter))
            .Where(x => IsFCStatus == null || IsFCStatus == x.IsFCBuff)
            .Where(x => IsStackable == null || IsStackable == x.IsStackable)
            .Where(x => ClassJobSelector.SelectedJobs.Count == 0 || ClassJobSelector.SelectedJobs.Any(j => j.IsValidJob(x.ClassJobCategory)));

        if (Services.Configuration.IconSortOption == SortOption.Alphabetical)   AvailableIcons = AvailableIcons.OrderBy(x => x.Name);
        if (Services.Configuration.IconSortOption == SortOption.Numerical)      AvailableIcons = AvailableIcons.OrderBy(x => x.IconID);

        AvailableIconsFav = AvailableIcons.Where(x => Services.Configuration.FavIcons.Contains(x.IconID));
        AvailableIconsPos = AvailableIcons.Where(x => x.Type == StatusType.Positive);
        AvailableIconsNeg = AvailableIcons.Where(x => x.Type == StatusType.Negative);
        AvailableIconsSpec = AvailableIcons.Where(x => x.Type == StatusType.Special);
    }

    void IconTable(string headerName, IEnumerable<IconInfo> availableIcons)
    {
        if (!ImGui.CollapsingHeader(headerName)) return;

        if (!availableIcons.Any())
        {
            ImGuiEx.Text(EColor.RedBright, $"There are no elements that match filter conditions.");
        }

        DrawIconTable(availableIcons);
    }

    void DrawIconTable(IEnumerable<IconInfo> infos)
    {
        if (selectedMoodle == null) return;

        int cols = Math.Clamp((int)(ImGui.GetWindowSize().X / 200f), 1, 10);

        if (!ImGui.BeginTable($"StatusTable##{WindowHandler.InternalCounter}", cols, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame)) return;

        for (int i = 0; i < cols; i++)
        {
            ImGui.TableSetupColumn($"Col{i}");
        }

        int index = 0;
        foreach (IconInfo info in infos)
        {
            if (index % cols == 0)
            {
                ImGui.TableNextRow();
            }

            index++;

            ImGui.TableNextColumn();

            if (!DalamudServices.TextureProvider.TryGetFromGameIcon(info.IconID, out ISharedImmediateTexture? iconTexture)) continue;
            if (!iconTexture.TryGetWrap(out IDalamudTextureWrap? wrap, out _)) continue;

            ImGui.Image(wrap.Handle, PluginConstants.StatusIconSize);
            
            ImGui.SameLine();

            ImGuiEx.Tooltip($"{info.IconID}");

            if (ImGui.RadioButton($"{info.Name}##{info.IconID}", selectedMoodle.IconID == info.IconID))
            {
                IconInfo? oldInfo = Services.MoodlesCache.GetStatusIconInfo((uint)selectedMoodle.IconID);
                if (Services.Configuration.AutoFill)
                {
                    if (selectedMoodle.Title.Length == 0 || selectedMoodle.Title == oldInfo?.Name)                      selectedMoodle.SetTitle(info.Name);
                    if (selectedMoodle.Description.Length == 0 || selectedMoodle.Description == oldInfo?.Description)   selectedMoodle.SetDescription(info.Description);
                }
                selectedMoodle.SetIconID((int)info.IconID, Mediator);
                PluginLog.LogVerbose($"Set Icon To: {info.IconID}");
            }
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            bool containsIcon = Services.Configuration.FavIcons.Contains(info.IconID);
            ImGuiEx.Text(containsIcon ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3, "\uf005");
            if (ImGuiEx.HoveredAndClicked())
            {
                if (containsIcon)
                {
                    Services.Configuration.FavIcons.Remove(info.IconID);
                }
                else
                {
                    Services.Configuration.FavIcons.Add(info.IconID);
                }
            }

            ImGui.PopFont();
        }

        ImGui.EndTable();
    }

    public void Open(IMoodle moodle)
    {
        selectedMoodle = moodle;
        IsOpen = true;
    }
}
