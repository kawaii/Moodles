using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.SimpleGui;
using Lumina.Excel.GeneratedSheets;
using Moodles.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace Moodles.Gui;
public class StatusSelector : Window
{
    MyStatus Delegate;

    bool? IsFCStatus = null;
    bool? IsStackable = null;
    List<Job> Jobs = [];
    string Filter = "";
    public List<uint> IconArray = [];

    bool Valid => Delegate != null && C.SavedStatuses.Contains(Delegate);

    public StatusSelector() : base("Select icon")
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
            ImGuiEx.Text(EColor.RedBright, "Edited status does not exists anymore. ");
        }

        var statusInfos = IconArray.Select(Utils.GetIconInfo).Where(x => x.HasValue).Cast<IconInfo>();

        ImGuiEx.SetNextItemWidthScaled(150f);
        ImGui.InputTextWithHint("##search", "Filter...", ref Filter, 50);
        ImGui.SameLine();
        ImGuiEx.Checkbox("FC buffs", ref this.IsFCStatus);
        ImGui.SameLine();
        ImGuiEx.Checkbox("Stackable", ref this.IsStackable);
        ImGui.SameLine();
        ImGuiEx.Text("Class/Job:");
        ImGui.SameLine();
        ImGuiEx.SetNextItemWidthScaled(150f);
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

        if (ImGui.BeginChild("child"))
        {
            if (ImGui.CollapsingHeader("Positive statuses"))
            {
                DrawIconTable(statusInfos.Where(x => x.Type == StatusType.Positive).OrderBy(x => x.IconID));
            }
            if (ImGui.CollapsingHeader("Negative statuses"))
            {
                DrawIconTable(statusInfos.Where(x => x.Type == StatusType.Negative).OrderBy(x => x.IconID));
            }
            if (ImGui.CollapsingHeader("Special statuses"))
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
        if (!infos.Any())
        {
            ImGuiEx.Text(EColor.RedBright, $"There are no elements that match filter conditions.");
        }
        int cols = Math.Clamp((int)(ImGui.GetWindowSize().X / 200f.Scale()), 1, 10);
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
                        Delegate.IconID = (int)info.IconID;
                    }
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
