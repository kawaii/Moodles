using ECommons.ExcelServices;
using ECommons.SimpleGui;
using Lumina.Excel.GeneratedSheets;
using Moodles.Data;

namespace Moodles.Gui;
public class StatusSelector : Window
{
    public MyStatus Delegate;

    bool? IsFCStatus = null;
    bool? IsStackable = null;
    List<Job> Jobs = [];
    string Filter = "";
    public List<uint> IconArray = [];
    bool Fullscreen = false;

    bool Valid => Delegate != null && C.SavedStatuses.Contains(Delegate);

    public StatusSelector() : base("Select Icon")
    {
        this.SetMinSize();
        foreach (var x in Svc.Data.GetExcelSheet<Status>())
        {
            if (IconArray.Contains(x.Icon)) continue;
            if (x.Icon == 0) continue;
            if (x.Name.ExtractText().IsNullOrEmpty()) continue;
            IconArray.Add(x.Icon);
        }
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        if (!Valid)
        {
            ImGuiEx.Text(EColor.RedBright, "Edited status no longer seems to exist.");
        }

        var statusInfos = IconArray.Select(Utils.GetIconInfo).Where(x => x.HasValue).Cast<IconInfo>();

        ImGui.SetNextItemWidth(150f);
        ImGui.InputTextWithHint("##search", "Filter...", ref Filter, 50);
        ImGui.SameLine();
        ImGui.Checkbox("Prefill Data", ref C.AutoFill);
        ImGuiEx.HelpMarker("Prefills the Title and Description inputs with data from the game itself regarding the icon. Requires those fields to be empty or unchanged from previous prefill data.");
        ImGui.SameLine();
        ImGuiEx.Checkbox("Stackable", ref this.IsStackable);
        ImGuiEx.HelpMarker("Toggles the filter between all status effecs, those with stacks only, and those without any stacks at all.");
        ImGui.SameLine();
        ImGuiEx.Text("Class/Job:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120f);
        if (ImGui.BeginCombo("##job", Jobs.Select(x => x.ToString().Replace("_", " ")).PrintRange(out var fullList)))
        {
            foreach (var cond in Enum.GetValues<Job>().Where(x => !x.IsUpgradeable()).OrderByDescending(x => Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)x).Role))
            {
                if (cond == Job.ADV) continue;
                var name = cond.ToString().Replace("_", " ");
                if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)cond.GetIcon(), false, out var texture))
                {
                    ImGui.Image(texture.ImGuiHandle, TabAutomation.JobIconSize);
                    ImGui.SameLine();
                }
                ImGuiEx.CollectionCheckbox(name, cond, Jobs);
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        ImGuiEx.Text("Sorting:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        ImGuiEx.EnumCombo("##order", ref C.IconSortOption);

        if (ImGui.BeginChild("child"))
        {
            if(C.FavIcons.Count > 0)
            {
                if (ImGui.CollapsingHeader("Favourites"))
                {
                    DrawIconTable(statusInfos.Where(x => C.FavIcons.Contains(x.IconID)).OrderBy(x => x.IconID));
                }
            }
            if (ImGui.CollapsingHeader("Positive Status Effects"))
            {
                DrawIconTable(statusInfos.Where(x => x.Type == StatusType.Positive).OrderBy(x => x.IconID));
            }
            if (ImGui.CollapsingHeader("Negative Status Effects"))
            {
                DrawIconTable(statusInfos.Where(x => x.Type == StatusType.Negative).OrderBy(x => x.IconID));
            }
            if (ImGui.CollapsingHeader("Special Status Effects"))
            {
                DrawIconTable(statusInfos.Where(x => x.Type == StatusType.Special).OrderBy(x => x.IconID));
            }
        }
        ImGui.EndChild();
    }

    void DrawIconTable(IEnumerable<IconInfo> infos)
    {
        infos = infos
            .Where(x => Filter == "" || (x.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase) || x.IconID.ToString().Contains(Filter)))
            .Where(x => IsFCStatus == null || IsFCStatus == x.IsFCBuff)
            .Where(x => IsStackable == null || IsStackable == x.IsStackable)
            .Where(x => Jobs.Count == 0 || (Jobs.Any(j => x.ClassJobCategory.IsJobInCategory(j.GetUpgradedJob()) || x.ClassJobCategory.IsJobInCategory(j.GetDowngradedJob())) && x.ClassJobCategory.RowId > 1));
        if (C.IconSortOption == SortOption.Alphabetical) infos = infos.OrderBy(x => x.Name);
        if (C.IconSortOption == SortOption.Numerical) infos = infos.OrderBy(x => x.IconID);
        if (!infos.Any())
        {
            ImGuiEx.Text(EColor.RedBright, $"There are no elements that match filter conditions.");
        }
        int cols = Math.Clamp((int)(ImGui.GetWindowSize().X / 200f), 1, 10);
        if(ImGui.BeginTable("StatusTable", cols, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
        {
            for (int i = 0; i < cols; i++)
            {
                ImGui.TableSetupColumn($"Col{i}");
            }
            int index = 0;
            foreach (var info in infos)
            {
                if (index % cols == 0) ImGui.TableNextRow();
                index++;
                ImGui.TableNextColumn();
                if(ThreadLoadImageHandler.TryGetIconTextureWrap(info.IconID, false, out var tex))
                {
                    ImGui.Image(tex.ImGuiHandle, UI.StatusIconSize);
                    ImGui.SameLine();
                    ImGuiEx.Tooltip($"{info.IconID}");
                    if (ImGui.RadioButton($"{info.Name}##{info.IconID}", Delegate.IconID == info.IconID))
                    {
                        var oldInfo = Utils.GetIconInfo((uint)Delegate.IconID);
                        if (C.AutoFill)
                        {
                            if (Delegate.Title.Length == 0 || Delegate.Title == oldInfo?.Name) Delegate.Title = info.Name;
                            if (Delegate.Description.Length == 0 || Delegate.Description == oldInfo?.Description) Delegate.Description = info.Description;
                        }
                        Delegate.IconID = (int)info.IconID;
                    }
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    var col = C.FavIcons.Contains(info.IconID);
                    ImGuiEx.Text(col ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3, "\uf005");
                    if (ImGuiEx.HoveredAndClicked())
                    {
                        C.FavIcons.Toggle(info.IconID);
                    }
                    ImGui.PopFont();
                }
            }
            ImGui.EndTable();
        }
    }

    public void Open(MyStatus status)
    {
        Delegate = status;
        this.IsOpen = true;
    }
}
