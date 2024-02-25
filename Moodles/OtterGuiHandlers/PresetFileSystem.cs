using ECommons.Configuration;
using Moodles.Data;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Filesystem;
using OtterGui.FileSystem.Selector;
using System.IO;

namespace Moodles.OtterGuiHandlers;
public sealed class PresetFileSystem : FileSystem<Preset>, IDisposable
{
    string FilePath = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "PresetFileSystem.json");
    public readonly PresetFileSystem.FileSystemSelector Selector;
    public PresetFileSystem(OtterGuiHandler h)
    {
        EzConfig.OnSave += Save;
        try
        {
            var info = new FileInfo(FilePath);
            if (info.Exists)
            {
                this.Load(info, C.SavedPresets, ConvertToIdentifier, ConvertToName);
            }
            Selector = new(this, h);
        }
        catch (Exception e)
        {
            e.Log();
        }
    }
    public bool TryGetPathByID(Guid id, out string path)
    {
        if (FindLeaf(C.SavedPresets.FirstOrDefault(x => x.GUID == id), out var leaf))
        {
            path = leaf.FullName();
            return true;
        }
        path = default;
        return false;
    }

    public void Dispose()
    {
        EzConfig.OnSave -= Save;
    }

    public void DoDelete(Preset item)
    {
        PluginLog.Debug($"Deleting {item.ID}");
        C.SavedPresets.Remove(item);
        if (FindLeaf(item, out var leaf))
        {
            this.Delete(leaf);
        }
        this.Save();
    }

    public bool FindLeaf(Preset item, out Leaf leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<Preset>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == item);
        return leaf != null;
    }

    private string ConvertToName(Preset item)
    {
        PluginLog.Debug($"Request conversion of {item.ID} to name");
        return $"Unnamed " + item.ID;
    }

    private string ConvertToIdentifier(Preset item)
    {
        PluginLog.Debug($"Request conversion of {item.ID} to identifier");
        return item.ID;
    }

    public void Save()
    {
        try
        {
            using var FileStream = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var StreamWriter = new StreamWriter(FileStream);
            this.SaveToFile(StreamWriter, SaveConverter, true);
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Error saving PresetFileSystem:");
            ex.Log();
        }
    }

    private (string, bool) SaveConverter(Preset item, string arg2)
    {
        PluginLog.Debug($"Saving {item.ID}");
        return (item.ID, true);
    }

    public class FileSystemSelector : FileSystemSelector<Preset, FileSystemSelector.State>
    {
        public List<uint> IconArray = [];
        string NewName = "";
        string ClipboardText = null;
        Preset CloneItem = null;
        public override ISortMode<Preset> SortMode => ISortMode<Preset>.FoldersFirst;
        static PresetFileSystem FS => P.OtterGuiHandler.PresetFileSystem;
        public FileSystemSelector(PresetFileSystem fs, OtterGuiHandler h) : base(fs, Svc.KeyState, h.Logger, (e) => e.Log())
        {
            AddButton(NewItem, 0);
            //AddButton(ImportButton, 10); needs custom logic
            //AddButton(CopyToClipboardButton, 20);
            AddButton(DeleteButton, 1000);
        }

        protected override uint CollapsedFolderColor => ImGuiColors.DalamudViolet.ToUint();
        protected override uint ExpandedFolderColor => CollapsedFolderColor;

        private void CopyToClipboardButton(Vector2 vector)
        {
            if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Copy.ToIconString(), vector, "Copy to clipboard.", Selected == null, true)) return;
            if (this.Selected != null)
            {
                var copy = this.Selected.JSONClone();
                copy.GUID = Guid.Empty;
                Copy(EzConfig.DefaultSerializationFactory.Serialize(copy, false));
            }
        }

        private void ImportButton(Vector2 size)
        {
            if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.FileImport.ToIconString(), size, "Try to import a profile from your clipboard.", false,
                    true))
                return;

            try
            {
                CloneItem = null;
                ClipboardText = Paste();
                ImGui.OpenPopup("##NewItem");
            }
            catch
            {
                Notify.Error("Could not import data from clipboard.");
            }
        }

        private void DeleteButton(Vector2 vector)
        {
            DeleteSelectionButton(vector, DoubleModifier.NoKey, "preset", "presets", FS.DoDelete);
        }

        private void NewItem(Vector2 size)
        {
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), size, "Create new preset", false,
                    true))
            {
                ClipboardText = null;
                CloneItem = null;
                ImGui.OpenPopup("##NewItem");
            }
        }

        private void DrawNewItemPopup()
        {
            if (!ImGuiUtil.OpenNameField("##NewItem", ref NewName))
                return;

            if (NewName == "")
            {
                Notify.Error($"Name can not be empty!");
                return;
            }

            if (ClipboardText != null)
            {

            }
            else if (CloneItem != null)
            {

            }
            else
            {
                try
                {
                    var newItem = new Preset();
                    C.SavedPresets.Add(newItem);
                    FS.CreateLeaf(FS.Root, NewName, newItem);
                }
                catch (Exception e)
                {
                    e.LogVerbose();
                    Notify.Error($"This name already exists!");
                }
            }

            NewName = string.Empty;
        }

        protected override void DrawPopups()
        {
            DrawNewItemPopup();
        }

        public record struct State { }

        protected override bool ApplyFilters(IPath path)
        {
            return FilterValue.Length > 0 && !path.FullName().Contains(this.FilterValue, StringComparison.OrdinalIgnoreCase);
        }

    }
}
