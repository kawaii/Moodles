using Dalamud.Interface.Utility.Raii;
using System.Runtime.CompilerServices;

namespace Moodles.Gui;

// From Brio
public class ImEtheirys
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float GetRemainingWidth()
    {
        return ImGui.GetContentRegionAvail().X;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float GetLineHeight()
    {
        return ImGui.GetTextLineHeight() + (ImGui.GetStyle().FramePadding.Y * 2);
    }

    public static bool ButtonSelectorStrip(string id, Vector2 size, ref int selected, string[] options)
    {
        if (size == Vector2.Zero) size = new Vector2(GetRemainingWidth(), GetLineHeight());

        bool changed = false;
        float buttonWidth = size.X / options.Length;

        using (ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Tab)))
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding))
            {
                using var child = ImRaii.Child(id, size, false, ImGuiWindowFlags.NoScrollbar);
                if (child.Success)
                {
                    using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0)))
                    {
                        for (int i = 0; i < options.Length; i++)
                        {
                            if (i > 0)
                                ImGui.SameLine();

                            bool val = i == selected;
                            ToggleStripButton($"{options[i]}##{id}", new(buttonWidth, size.Y), ref val, false);

                            if (val && i != selected)
                            {
                                selected = i;
                                changed = true;
                            }
                        }
                    }
                }
            }
        }

        return changed;
    }

    public static bool ToggleStripButton(string label, Vector2 size, ref bool selected, bool canSelect = true)
    {
        bool clicked = false;

        using (ImRaii.Disabled(canSelect && selected))
        {
            using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(selected ? ImGuiCol.TabActive : ImGuiCol.Tab)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0)))
                if (ImGui.Button(label, size))
                {
                    selected = !selected;
                    clicked = true;
                }
        }

        return clicked;
    }
}
