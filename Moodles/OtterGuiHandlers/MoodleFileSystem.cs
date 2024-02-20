using ECommons.Configuration;
using OtterGui.Classes;
using OtterGui;
using OtterGui.Filesystem;
using OtterGui.FileSystem.Selector;
using System.IO;
using Lumina.Excel.GeneratedSheets;
using Moodles.Data;
using Dalamud.Interface.Internal.Notifications;

namespace Moodles.OtterGuiHandlers;
public sealed class MoodleFileSystem : FileSystem<MyStatus> , IDisposable
{
    string FilePath = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "MoodleFileSystem.json");
    public readonly MoodleFileSystem.FileSystemSelector Selector;
    public MoodleFileSystem(OtterGuiHandler h)
    {
        EzConfig.OnSave += Save;
        try
        {
            var info = new FileInfo(FilePath);
            if (info.Exists)
            {
                this.Load(info, C.SavedStatuses, ConvertToIdentifier, ConvertToName);
            }
            Selector = new(this, h);
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    public void Dispose()
    {
        EzConfig.OnSave -= Save;
    }

    public void DoDelete(MyStatus status)
    {
        PluginLog.Debug($"Deleting {status.ID}");
        C.SavedStatuses.Remove(status);
        if(FindLeaf(status, out var leaf))
        {
            this.Delete(leaf);
        }
        this.Save();
    }

    public bool FindLeaf(MyStatus status, out Leaf leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<MyStatus>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == status);
        return leaf != null;
    }

    public bool TryGetPathByID(Guid id, out string path)
    {
        if (FindLeaf(C.SavedStatuses.FirstOrDefault(x => x.GUID == id), out var leaf))
        {
            path = leaf.FullName();
            return true;
        }
        path = default;
        return false;
    }

    private string ConvertToName(MyStatus status)
    {
        PluginLog.Debug($"Request conversion of {status.Title} {status.ID} to name");
        return $"Unnamed " + status.ID;
    }

    private string ConvertToIdentifier(MyStatus status)
    {
        PluginLog.Debug($"Request conversion of {status.Title} {status.ID} to identifier");
        return status.ID;
    }

    public void Save()
    {
        try
        {
            using var FileStream = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var StreamWriter = new StreamWriter(FileStream);
            this.SaveToFile(StreamWriter, SaveConverter, true);
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Error saving MoodleFileSystem:");
            ex.Log();
        }
    }

    private (string, bool) SaveConverter(MyStatus status, string arg2)
    {
        PluginLog.Debug($"Saving {status.Title}  {status.ID}");
        return (status.ID, true);
    }

    public class FileSystemSelector : FileSystemSelector<MyStatus, FileSystemSelector.State>
    {
        string NewName = "";
        string ClipboardText = null;
        MyStatus CloneStatus = null;
        public override ISortMode<MyStatus> SortMode => ISortMode<MyStatus>.FoldersFirst;
        static MoodleFileSystem FS => P.OtterGuiHandler.MoodleFileSystem;
        public FileSystemSelector(MoodleFileSystem fs, OtterGuiHandler h) : base(fs, Svc.KeyState, h.Logger, (e) => e.Log())
        {
            AddButton(NewMoodleButton, 0);
            AddButton(ImportButton, 10);
            AddButton(CopyToClipboardButton, 20);
            AddButton(DeleteButton, 1000);
        }

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
            if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.FileImport.ToIconString(), size, "Try to import a moodle from your clipboard.", false,
                    true))
                return;

            try
            {
                CloneStatus = null;
                ClipboardText = Paste();
                ImGui.OpenPopup("##NewMoodle");
            }
            catch
            {
                Notify.Error("Could not import data from clipboard.");
            }
        }

        private void DeleteButton(Vector2 vector)
        {
            DeleteSelectionButton(vector, DoubleModifier.NoKey, "moodle", "moodles", FS.DoDelete);
        }

        private void NewMoodleButton(Vector2 size)
        {
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), size, "Create new status", false,
                    true))
            {
                ClipboardText = null;
                CloneStatus = null;
                ImGui.OpenPopup("##NewMoodle");
            }
        }

        private void DrawNewMoodlePopup()
        {
            if (!ImGuiUtil.OpenNameField("##NewMoodle", ref NewName))
                return;

            if (NewName == "")
            {
                Notify.Error($"Name can not be empty!");
                return;
            }

            if (ClipboardText != null)
            {
                try
                {
                    var newStatus = EzConfig.DefaultSerializationFactory.Deserialize<MyStatus>(ClipboardText);
                    FS.CreateLeaf(FS.Root, NewName, newStatus);
                    C.SavedStatuses.Add(newStatus);
                }
                catch (Exception e)
                {
                    e.LogVerbose();
                    Notify.Error($"Error: {e.Message}");
                }
            }
            else if (CloneStatus != null)
            {

            }
            else
            {
                try
                {
                    var newStatus = new MyStatus();
                    FS.CreateLeaf(FS.Root, NewName, newStatus);
                    C.SavedStatuses.Add(newStatus);
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
            DrawNewMoodlePopup();
        }

        public record struct State { }
    }
}
