using Dalamud.Interface.Textures;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.TempWindowing;
using System;
using System.Collections.Generic;

namespace Moodles.Moodles.OtterGUIHandlers.Selectors;

internal class ClassJobSelector
{
    public readonly List<SelectableJob> SelectedJobs = [];
    readonly SelectableJob[] SelectableJobs = [];

    readonly IMoodlesServices Services;

    public ClassJobSelector(IMoodlesServices services, DalamudServices dalamudServices)
    {
        Services = services;

        List<SelectableJob> tempClassJobs = new List<SelectableJob>();

        foreach (ClassJob job in Services.Sheets.FilterableJobs)
        {
            tempClassJobs.Add(new SelectableJob(dalamudServices, job));
        }

        SelectableJobs = tempClassJobs.ToArray();
    }

    public bool Draw(float width = 120f)
    {
        bool changed = false;

        ImGuiEx.Text("Class/Job:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(width);

        if (ImGui.BeginCombo($"##job##jobselect{WindowHandler.InternalCounter}", SelectorShower()))
        {
            foreach (SelectableJob sJob in SelectableJobs)
            {
                ImGui.Image(sJob.JobIcon.GetWrapOrEmpty().Handle, PluginConstants.JobIconSize);
                ImGui.SameLine();
                
                changed |= ImGuiEx.CollectionCheckbox(sJob.Abbreviation, sJob, SelectedJobs);
            }
            ImGui.EndCombo();
        }

        return changed;
    }

    public string SelectorShower()
    {
        string[] jobs = SelectedJobAbbreviations();

        int size = jobs.Length;

        if (size == 0) return "Any";
        if (size == 1) return jobs[0];

        return $"{size} selected";
    }

    public string[] SelectedJobAbbreviations()
    {
        int size = SelectedJobs.Count;

        string[] abbreviations = new string[size];

        for (int i = 0; i < size; i++)
        {
            abbreviations[i] = SelectedJobs[i].Abbreviation;
        }

        return abbreviations;
    }
}

internal class SelectableJob
{
    public readonly ISharedImmediateTexture JobIcon;
    public readonly string Abbreviation;
    public readonly ClassJob ClassJob;

    public SelectableJob(DalamudServices dalamudServices, ClassJob classJob)
    {
        ClassJob = classJob;
        Abbreviation = classJob.Abbreviation.ExtractText();
        JobIcon = dalamudServices.TextureProvider.GetFromGameIcon(classJob.RowId + 062100);
    }

    public bool IsValidJob(ClassJobCategory otherJob)
    {
        if (otherJob.RowId == ClassJob.ClassJobCategory.RowId) return true;
        if (otherJob.RowId == ClassJob.ClassJobParent.Value.ClassJobCategory.RowId) return true;

        return false;
    }
}
