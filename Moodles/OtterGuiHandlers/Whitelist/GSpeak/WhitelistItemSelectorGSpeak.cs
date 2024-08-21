using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using ECommons.GameHelpers;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Moodles.OtterGuiHandlers.Whitelist.GSpeak;
#nullable enable
public class WhitelistItemSelectorGSpeak<T>
{
    [Flags]
    public enum Flags : byte
    {
        None = 0x00, // Just a list
        Delete = 0x01, // Add a delete button for the current element (Trashcan), uses OnDelete
        Add = 0x02, // Add an Add button to create a new empty element given a name (Plus), uses OnAdd
        Import = 0x04, // Add an Import button to create a new element from the string in your clipboard (Clipboard), uses OnImport
        Duplicate = 0x08, // Add a Duplicate button to create copy of the currently selected element given a name, uses OnDuplicate

        Filter = 0x10, // Add a filter up top that filters the visible elements and uses Filtered.
        Move = 0x20, // Items can be moved by drag and drop, uses OnMove.
        Drop = 0x40, // Items can be dropped onto, uses OnDrop. Create Sources with CreateDropSource.

        ButtonMask = 0x0F,
        All = 0x7F,
    }

    // Initial setup.
    protected readonly IList<T> Items;
    private readonly Flags _flags;
    private readonly byte _numButtons;

    // Used by Filter
    // Indices of the currently available items after Filtered.
    protected readonly List<int> FilteredItems;
    protected string Filter = string.Empty;
    protected bool FilterDirty = true;
    private int _lastSize;

    // Used by Add, Duplicate and Import buttons
    private string _newName = string.Empty;

    // Used by Move and Drop
    private object? _dragDropData;

    // Labels
    private readonly string _label = "##ItemSelector";

    public string Label
    {
        get => _label;
        init
        {
            _label = value;
            DragDropLabel = $"{value}DragDrop";
            MoveLabel = $"{value}Move";
        }
    }

    public readonly string DragDropLabel = "##ItemSelectorDragDrop";
    public readonly string MoveLabel = "##ItemSelectorMove";

    public WhitelistItemSelectorGSpeak(IList<T> items, Flags flags = Flags.None)
    {
        Items = items;
        _lastSize = Items.Count;
        _flags = flags;
        FilteredItems = Enumerable.Range(0, Items.Count).ToList();
        _numButtons = (byte)(_flags & Flags.ButtonMask) switch
        {
            0x01 => 1,
            0x02 => 1,
            0x03 => 2,
            0x04 => 1,
            0x05 => 2,
            0x06 => 2,
            0x07 => 3,
            0x08 => 1,
            0x09 => 2,
            0x0A => 2,
            0x0B => 3,
            0x0C => 3,
            0x0D => 3,
            0x0E => 3,
            0x0F => 4,
            _ => 0,
        };
        _numButtons += 2;
        Label = "##ItemSelector";
        SetCurrent(0);
    }

    public void CreateDropSource<TData>(TData data, string tooltip)
    {
        using var source = ImRaii.DragDropSource();
        if (!source)
            return;

        _dragDropData = data;
        ImGui.SetDragDropPayload(DragDropLabel, nint.Zero, 0);
        ImGui.TextUnformatted(tooltip);
    }

    public bool CreateDropTarget<TData>(Action<TData> action)
    {
        using var target = ImRaii.DragDropTarget();
        if (!target)
            return false;

        if (!ImGuiUtil.IsDropping(DragDropLabel))
            return false;

        if (_dragDropData is not TData data)
            return false;

        action(data);
        return true;
    }

    public bool CreateDropTarget<TData>(Func<TData, bool> func)
    {
        using var target = ImRaii.DragDropTarget();
        if (!target)
            return false;

        if (!ImGuiUtil.IsDropping(DragDropLabel))
            return false;

        return _dragDropData is TData data && func(data);
    }

    public void SetFilterDirty()
        => FilterDirty = true;

    // Selection
    public int CurrentIdx = 0;
    public T? Current { get; private set; }

    protected void ClearCurrentSelection()
    {
        CurrentIdx = -1;
        Current = default;
    }

    public void TryRestoreCurrent()
    {
        CurrentIdx = Current == null ? -1 : Items.IndexOf(Current);
        if (CurrentIdx == -1)
            Current = default;
    }

    private void SetCurrent(int idx)
    {
        if (idx < Items.Count)
        {
            CurrentIdx = idx;
            Current = Items[idx];
        }
        else if (Items.Count > 0)
        {
            CurrentIdx = Items.Count - 1;
            Current = Items.Last();
        }
        else
        {
            ClearCurrentSelection();
        }
    }

    protected void SetCurrent(T item)
    {
        var idx = Items.IndexOf(item);
        if (idx >= 0)
            SetCurrent(idx);
    }

    public T? EnsureCurrent()
    {
        TryRestoreCurrent();
        if (Current == null)
            SetCurrent(0);

        return Current;
    }


    // Customization points.
    protected virtual bool OnDraw(int idx)
        => throw new NotImplementedException();

    protected virtual bool Filtered(int idx)
        => throw new NotImplementedException();

    protected virtual bool OnDelete(int idx)
        => throw new NotImplementedException();

    protected virtual bool OnAdd(string name)
        => throw new NotImplementedException();

    protected virtual bool OnMove(int idx1, int idx2)
        => throw new NotImplementedException();

    protected virtual bool OnClipboardImport(string name, string data)
        => throw new NotImplementedException();

    protected virtual bool OnDuplicate(string name, int idx)
        => throw new NotImplementedException();

    protected virtual void OnDrop(object? data, int idx)
        => throw new NotImplementedException();

    private void InternalDraw(int idx)
    {
        // Add a slight distance from the border so that the padding of a selectable fills the whole border.
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);
        // Assume that OnDraw functions like a Selectable, if it returns true, select the value.
        if (OnDraw(idx) && idx != CurrentIdx)
        {
            CurrentIdx = idx;
            Current = Items[idx];
        }

        // If the ItemSelector supports Move, every item is a Move-DragDropSource. The data is the index of the dragged element.
        if (_flags.HasFlag(Flags.Move))
        {
            using var source = ImRaii.DragDropSource();
            if (source)
            {
                _dragDropData = idx;
                ImGui.SetDragDropPayload(MoveLabel, nint.Zero, 0);
                ImGui.TextUnformatted($"Reordering {idx + 1}...");
            }
        }

        // If the ItemSelector supports Move or Drop, every item is a DragDropTarget.
        if ((_flags & (Flags.Move | Flags.Drop)) == Flags.None)
            return;

        using var target = ImRaii.DragDropTarget();
        if (!target)
            return;

        // Handle drops.
        if (ImGuiUtil.IsDropping(DragDropLabel))
        {
            OnDrop(_dragDropData, idx);
        }
        else if (ImGuiUtil.IsDropping(MoveLabel))
        {
            var oldIdx = (int)(_dragDropData ?? idx);
            if (OnMove(oldIdx, idx) && oldIdx == CurrentIdx)
                SetCurrent(idx);
        }
    }

    private void DrawFilter(float width)
    {
        if (!_flags.HasFlag(Flags.Filter))
            return;

        var newFilter = Filter;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
        ImGui.SetNextItemWidth(width);
        var enterPressed = ImGui.InputTextWithHint(string.Empty, "Filter...", ref newFilter, 64, ImGuiInputTextFlags.EnterReturnsTrue);
        if (newFilter != Filter)
        {
            Filter = newFilter;
            FilterDirty = true;
        }

        // Select the topmost item of the filtered list on enter.
        if (enterPressed)
        {
            UpdateFilteredItems();
            if (FilteredItems.Count > 0)
                SetCurrent(FilteredItems.First());
        }

        style.Pop();
    }

    // Update filtered items whenever the size of the base collection changes
    // or the filter was set to dirty for any reason.
    private void UpdateFilteredItems()
    {
        if (_lastSize != Items.Count)
        {
            FilterDirty = true;
            _lastSize = Items.Count;
        }

        if (!FilterDirty)
            return;

        FilteredItems.Clear();
        for (var idx = 0; idx < Items.Count; ++idx)
        {
            if (!Filtered(idx))
                FilteredItems.Add(idx);
        }

        FilterDirty = false;
    }

    public void Draw(float width)
    {
        using var id = ImRaii.PushId(Label);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var group = ImRaii.Group();
        using var child = ImRaii.Child(string.Empty, new Vector2(width, 0), true);
        if (!child)
            return;

        style.Pop();

        DrawFilter(width);
        UpdateFilteredItems();
        ImGuiClip.ClippedDraw(FilteredItems, InternalDraw, ImGui.GetTextLineHeightWithSpacing());
        style.Push(ImGuiStyleVar.FrameRounding, 0)
            .Push(ImGuiStyleVar.WindowPadding, Vector2.Zero)
            .Push(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        child.Dispose();
    }
}

public static class ItemDetailsWindow
{
    public static void Draw(string label, Action drawHeader, Action drawDetails)
    {
        using var group = ImRaii.Group();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var id = ImRaii.PushId(label);
        using var child = ImRaii.Child(string.Empty, ImGui.GetContentRegionAvail(), true, ImGuiWindowFlags.MenuBar);
        if (!child)
            return;

        style.Pop();

        if (ImGui.BeginMenuBar())
        {
            drawHeader();
            ImGui.EndMenuBar();
        }

        ImGui.Dummy(ImGui.GetStyle().WindowPadding * Vector2.UnitY);
        ImGui.Dummy(ImGui.GetStyle().WindowPadding);
        ImGui.SameLine();
        using var detailGroup = ImRaii.Group();
        drawDetails();
    }
}
