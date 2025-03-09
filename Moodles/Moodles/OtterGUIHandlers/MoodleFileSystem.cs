﻿using Dalamud.Interface.Colors;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Filesystem;
using OtterGui.FileSystem.Selector;
using OtterGui.Raii;
using System;
using System.IO;
using System.Linq;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Newtonsoft.Json;
using Moodles.Moodles.Services;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.OtterGUIHandlers;

internal sealed class MoodleFileSystem : FileSystem<Moodle>, IDisposable
{
    readonly string FilePath;
    public readonly FileSystemSelector? Selector;

    readonly DalamudServices DalamudServices;
    readonly OtterGuiHandler OtterGuiHandler;
    readonly IMoodlesServices Services;
    readonly IMoodlesDatabase Database;

    public MoodleFileSystem(DalamudServices dalamudServices, OtterGuiHandler otterGuiHandler, IMoodlesServices services, IMoodlesDatabase database)
    {
        DalamudServices = dalamudServices;
        Services = services;
        OtterGuiHandler = otterGuiHandler;
        Database = database;

        FilePath = Path.Combine(DalamudServices.DalamudPlugin.ConfigDirectory.FullName, "NewMoodleFileSystem.json");

        try
        {
            FileInfo info = new FileInfo(FilePath);
            if (info.Exists)
            {
                PluginLog.Log($"Trying to identify {info}");
                Load(info, services.Configuration.SavedMoodles, ConvertToIdentifier, ConvertToName);
            }
            Selector = new FileSystemSelector(this, otterGuiHandler, dalamudServices, services, Database);
        }
        catch (Exception e)
        {
            PluginLog.LogException(e);
        }
    }

    public void Dispose()
    {
        
    }

    public void DoDelete(Moodle moodle)
    {
        PluginLog.Log($"Deleting {moodle.Identifier}");

        Services.Configuration.SavedMoodles.Remove(moodle);

        if (FindLeaf(moodle, out Leaf? leaf))
        {
            Delete(leaf);
        }

        Save();
    }

    public bool FindLeaf(Moodle status, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<Moodle>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == status);
        return leaf != null;
    }

    public bool TryGetPathByID(Guid id, out string path)
    {
        path = string.Empty;

        Moodle? firstMoodle = Services.Configuration.SavedMoodles.FirstOrDefault(x => x.Identifier == id);
        if (firstMoodle == null) return false;

        if (!FindLeaf(firstMoodle, out Leaf? leaf))
        {
            return false;
        }

        path = leaf.FullName();
        return true;
    }

    string ConvertToName(Moodle status)
    {
        PluginLog.LogVerbose($"Request conversion of {status.Title} {status.Identifier} to name.");
        return $"Unnamed " + status.Identifier;
    }

    string ConvertToIdentifier(Moodle status)
    {
        PluginLog.LogVerbose($"Request conversion of {status.Title} {status.Identifier} to identifier");
        return status.Identifier.ToString();
    }

    public void Save()
    {
        try
        {
            using var FileStream = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var StreamWriter = new StreamWriter(FileStream);
            SaveToFile(StreamWriter, SaveConverter, true);
            Services.Configuration.Save(DalamudServices.DalamudPlugin);
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Error saving MoodleFileSystem:");
        }
    }

    (string, bool) SaveConverter(Moodle status, string arg2)
    {
        PluginLog.LogVerbose($"Saving {status.Title}  {status.Identifier}");
        return (status.Identifier.ToString(), true);
    }

    public class FileSystemSelector : FileSystemSelector<Moodle, FileSystemSelector.State>
    {
        string NewName = "";
        string? ClipboardText = null;

        public override ISortMode<Moodle> SortMode => ISortMode<Moodle>.FoldersFirst;

        readonly MoodleFileSystem MoodleFileSystem;
        readonly IMoodlesServices Services;
        readonly IMoodlesDatabase Database;

        public FileSystemSelector(MoodleFileSystem fileSystem, OtterGuiHandler otterGuiHandler, DalamudServices services, IMoodlesServices moodlesServices, IMoodlesDatabase database) : base(fileSystem, services.KeyState, otterGuiHandler.Logger, PluginLog.LogException)
        {
            MoodleFileSystem = fileSystem;
            Services = moodlesServices;
            Database = database;

            AddButton(NewMoodleButton, 0);
            AddButton(ImportButton, 10);
            AddButton(CopyToClipboardButton, 20);
            AddButton(DeleteButton, 1000);
        }

        protected override uint CollapsedFolderColor => ImGuiColors.DalamudViolet.ToUint();
        protected override uint ExpandedFolderColor => CollapsedFolderColor;

        protected override void DrawLeafName(Leaf leaf, in State state, bool selected)
        {
            ImGuiTreeNodeFlags flag = selected ? ImGuiTreeNodeFlags.Selected | LeafFlags : LeafFlags;
            using ImRaii.IEndObject _ = ImRaii.TreeNode(leaf.Name + $"                                                       ", flag);
        }

        void CopyToClipboardButton(Vector2 vector)
        {
            if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Copy.ToIconString(), vector, "Copy to clipboard.", Selected == null, true)) return;

            if (Selected == null) return;

            IMoodle? copy = Selected.JSONClone();
            if (copy == null) return;

            copy.SetIdentifier(Guid.Empty);
            ImGui.SetClipboardText(JsonConvert.SerializeObject(copy));
        }

        void ImportButton(Vector2 size)
        {
            if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.FileImport.ToIconString(), size, "Try to import a moodle from your clipboard.", false, true))
            {
                return;
            }

            try
            {
                ClipboardText = ImGui.GetClipboardText();
                ImGui.OpenPopup("##NewMoodle");
            }
            catch
            {
                PluginLog.LogFatal("Could not import data from clipboard.");
            }
        }

        void DeleteButton(Vector2 vector)
        {
            DeleteSelectionButton(vector, new DoubleModifier(ModifierHotkey.Control), "moodle", "moodles", MoodleFileSystem.DoDelete);
        }

        void NewMoodleButton(Vector2 size)
        {
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), size, "Create new status", false, true))
            {
                ClipboardText = null;
                ImGui.OpenPopup("##NewMoodle");
            }
        }

        void DrawNewMoodlePopup()
        {
            if (!ImGuiUtil.OpenNameField("##NewMoodle", ref NewName))
            {
                return;
            }

            if (NewName == "")
            {
                PluginLog.LogFatal($"Name can not be empty!");
                return;
            }

            if (ClipboardText != null)
            {
                try
                {
                    Moodle? newStatus = JsonConvert.DeserializeObject<Moodle>(ClipboardText);
                    if (newStatus != null)
                    {
                        MoodleFileSystem.CreateLeaf(MoodleFileSystem.Root, NewName, newStatus);
                        Services.Configuration.SavedMoodles.Add(newStatus);
                        MoodleFileSystem.Save();
                    }
                    else
                    {
                        PluginLog.LogFatal($"Invalid clipboard data");
                    }
                }
                catch (Exception e)
                {
                    PluginLog.LogFatal($"Error: {e.Message}");
                }
            }
            else
            {
                try
                {
                    IMoodle newStatus = Database.CreateMoodle();

                    if (newStatus is Moodle moodle)
                    {
                        MoodleFileSystem.CreateLeaf(FileSystem.Root, NewName, moodle);
                        Services.Configuration.SavedMoodles.Add(moodle);
                        MoodleFileSystem.Save();
                    }
                }
                catch (Exception e)
                {
                    PluginLog.LogFatal($"This name already exists! {e.Message}");
                }
            }

            NewName = string.Empty;
        }

        protected override void DrawPopups()
        {
            DrawNewMoodlePopup();
        }

        public record struct State { }

        protected override bool ApplyFilters(IPath path)
        {
            return FilterValue.Length > 0 && !path.FullName().Contains(FilterValue, StringComparison.OrdinalIgnoreCase);
        }

    }
}
