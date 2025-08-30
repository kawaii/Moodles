﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using Moodles.Data;

namespace Moodles.Gui;

public static unsafe class TabFuckup
{
    private static MyStatus Status = new();
    private static int Duration = 20;
    private static string Owner = "";
    private static int Cnt = 10;

    public static void Draw()
    {
        if (ImGui.BeginCombo("Select status manager", $"{Owner}"))
        {
            foreach (var x in C.StatusManagers)
            {
                if (ImGui.Selectable(x.Key))
                {
                    Owner = x.Key;
                }
            }
            ImGui.EndCombo();
        }

        if (ImGui.Button("Self"))
        {
            Owner = Player.NameWithWorld;
        }
        ImGui.SameLine();
        if (ImGui.Button("Target") && Svc.Targets.Target is IPlayerCharacter pct)
        {
            Owner = pct.GetNameWithWorld();
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f);
        if (ImGui.BeginCombo("##Players around", "Players around"))
        {
            foreach (var x in Svc.Objects)
            {
                if (x is IPlayerCharacter pc)
                {
                    if (ImGui.Selectable(pc.GetNameWithWorld())) Owner = pc.GetNameWithWorld();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f);
        if (ImGui.BeginCombo("##party", "Party"))
        {
            foreach (var x in Svc.Party)
            {
                if (x.GameObject is IPlayerCharacter pc)
                {
                    if (ImGui.Selectable(pc.GetNameWithWorld())) Owner = pc.GetNameWithWorld();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear all managers"))
        {
            C.StatusManagers.Clear();
        }
        if (C.StatusManagers.TryGetValue(Owner, out var manager))
        {
            if (ImGui.CollapsingHeader("Add##collap"))
            {
                var iconArray = new List<uint>();
                foreach (var x in Svc.Data.GetExcelSheet<Status>())
                {
                    if (iconArray.Contains(x.Icon)) continue;
                    if (x.Icon == 0) continue;
                    iconArray.Add(x.Icon);
                    if (x.MaxStacks > 1)
                    {
                        for (var i = 2; i < x.MaxStacks; i++)
                        {
                            iconArray.Add((uint)(x.Icon + i - 1));
                        }
                    }
                }
                ImGui.SetNextItemWidth(100f);
                if (ImGui.BeginCombo("##sel", $"Icon: {Status.IconID}", ImGuiComboFlags.HeightLargest))
                {
                    var cnt = 0;
                    foreach (var x in iconArray)
                    {
                        if (ThreadLoadImageHandler.TryGetIconTextureWrap(x, false, out var t))
                        {
                            ImGui.Image(t.Handle, new Vector2(24, 32));
                            if (ImGuiEx.HoveredAndClicked())
                            {
                                Status.IconID = (int)x;
                                ImGui.CloseCurrentPopup();
                            }
                            cnt++;
                            if (cnt % 20 != 0) ImGui.SameLine();
                        }
                    }
                    ImGui.Dummy(Vector2.One);
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputText("Name", ref Status.Title, 50);
                ImGui.SameLine();
                ImGuiEx.InputTextMultilineExpanding("Description", ref Status.Description, 500, 1, 10, 100);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt("Duration, s", ref Duration);
                ImGui.SetNextItemWidth(100f);
                ImGui.InputText("Applier", ref Status.Applier, 50);
                ImGui.SameLine();
                if (ImGui.Button("Me")) Status.Applier = Player.NameWithWorld;
                ImGui.SameLine();
                ImGuiEx.EnumCombo("Type", ref Status.Type);
                if (ImGui.Button("Add"))
                {
                    Status.GUID = Guid.NewGuid();
                    Status.ExpiresAt = Utils.Time + Duration * 1000;
                    if (Duration == 0) Status.ExpiresAt = long.MaxValue;
                    manager.AddOrUpdate(Status.JSONClone(), UpdateSource.StatusTuple);
                }
                ImGui.SameLine();
                if (ImGui.Button("Randomize and add"))
                {
                    Status.GUID = Guid.NewGuid();
                    Status.Title = $"Random status {Random.Shared.Next()}";
                    Status.IconID = (int)iconArray[Random.Shared.Next(iconArray.Count)];
                    Status.Type = (StatusType)Random.Shared.Next(3);
                    Status.Applier = Random.Shared.Next(2) == 0 ? Player.NameWithWorld : "";
                    Status.Seconds = Random.Shared.Next(5, 60);
                    if (Random.Shared.Next(20) == 0) Status.Minutes = Random.Shared.Next(5, 60);
                    if (Random.Shared.Next(100) == 0) Status.Hours = Random.Shared.Next(5, 60);
                    manager.AddOrUpdate(Status.JSONClone().PrepareToApply(), UpdateSource.StatusTuple);
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt($"Random buffs", ref Cnt);
                ImGui.SameLine();
                if (ImGui.Button("add"))
                {
                    for (var i = 0; i < Cnt; i++)
                    {
                        Status.GUID = Guid.NewGuid();
                        Status.Title = $"Random status {Random.Shared.Next()}";
                        Status.Description = $"Random status description {Random.Shared.Next()}\n {Random.Shared.Next()}\n {Random.Shared.Next()}";
                        Status.IconID = (int)iconArray[Random.Shared.Next(iconArray.Count)];
                        Status.Type = (StatusType)Random.Shared.Next(3);
                        Status.Applier = Random.Shared.Next(2) == 0 ? Player.NameWithWorld : "";
                        Status.Minutes = 0;
                        Status.Hours = 0;
                        Status.Seconds = Random.Shared.Next(5, 60);
                        if (Random.Shared.Next(20) == 0) Status.Minutes = Random.Shared.Next(5, 60);
                        if (Random.Shared.Next(100) == 0) Status.Hours = Random.Shared.Next(5, 60);
                        var array = Svc.Objects.Where(x => x is IPlayerCharacter pc && pc.IsTargetable).Cast<IPlayerCharacter>().ToArray();
                        Utils.GetMyStatusManager(array[Random.Shared.Next(array.Length)]).AddOrUpdate(Status.JSONClone().PrepareToApply(), UpdateSource.StatusTuple);
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Copy bin"))
                {
                    Copy(manager.BinarySerialize().ToHexString());
                }
                ImGui.SameLine();
                if (ImGui.Button("Apply bin") && TryParseByteArray(Paste(), out var a))
                {
                    manager.Apply(a, UpdateSource.DataString);
                }
                ImGui.Separator();
            }
            List<ImGuiEx.EzTableEntry> entries = [];
            foreach (var x in manager.Statuses)
            {
                entries.Add(new("", false, delegate
                {
                    if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)x.IconID, false, out var icon))
                    {
                        ImGui.Image(icon.Handle, new Vector2(24, 32) * 0.75f);
                    }
                }));
                entries.Add(new("Name", delegate
                {
                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputText($"##Name{x.ID}", ref x.Title, 50);
                }));
                entries.Add(new("Description", delegate
                {
                    ImGuiEx.InputTextMultilineExpanding($"##Description{x.ID}", ref x.Description, 150, 1, 10);
                }));
                entries.Add(new("Applier", delegate
                {
                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputText($"##Applier{x.ID}", ref x.Applier, 50);
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) x.Applier = Player.NameWithWorld;
                }));
                entries.Add(new("Expires", delegate
                {
                    ImGuiEx.SetNextItemFullWidth();
                    ImGuiEx.InputLong($"##Expires{x.ID}", ref x.ExpiresAt);
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) x.ExpiresAt = long.MaxValue;
                }));
                entries.Add(new("Type", false, delegate
                {
                    ImGuiEx.EnumCombo($"Type##{x.ID}", ref x.Type);
                }));
                entries.Add(new("Dispelable", false, delegate
                {
                    ImGui.Checkbox($"Dispel##{x.ID}", ref x.Dispelable);
                }));
                entries.Add(new("AddShown", false, delegate
                {
                    ImGuiEx.CollectionCheckbox($"AddShown##{x.ID}", x.GUID, manager.AddTextShown);
                }));
                entries.Add(new("RemoveShown", false, delegate
                {
                    ImGuiEx.CollectionCheckbox($"RemoveShown##{x.ID}", x.GUID, manager.RemTextShown);
                }));
                entries.Add(new("Ctrl", false, delegate
                {
                    if (ImGui.Button($"Del##{x.ID}"))
                    {
                        x.ExpiresAt = 0;
                    }
                }));
            }
            ImGuiEx.EzTable(null, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders, entries, false);
        }
        else
        {
            if (ImGui.Button("Add Manager"))
            {
                MyStatusManager statusManager = new MyStatusManager();

                C.StatusManagers[Owner] = statusManager;
            }
        }

        if (C.StatusManagers.TryGetValue(Owner, out var sm))
        {
            if (sm != null)
            {
                if (ImGui.Button("Remove Status Manager"))
                {
                    C.StatusManagers.Remove(Owner);
                }
            }
        }
    }
}
