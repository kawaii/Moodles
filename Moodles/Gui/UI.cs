using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.FlyText;
using ECommons.Configuration;
using ECommons.EzIpcManager;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.GeneratedSheets;
using Moodles.Data;
using System.IO;

namespace Moodles.Gui;
public unsafe static class UI
{
    static MyStatus Status = new();
    static int Duration = 20;
    static string Owner = "";
    public static bool Suppress = false;
    public static readonly Vector2 StatusIconSize = new(24, 32);
    static uint OID = 0;
    static FlyTextKind MessageID = FlyTextKind.Debuff;
    static uint a4 = 0;
    static uint a5 = 0;
    static uint a7 = 0;
    static uint a8 = 0;
    static uint StatusID = 0;
    static bool My = false;
    static int Cnt = 10;

    public static void Draw()
    {
        if (EzThrottler.Throttle("PeriodicConfigSave", 30 * 1000)) EzConfig.Save();
        ImGuiEx.EzTabBar("##main", [
            ("Moodles", TabMoodles.Draw, null, true),
            ("Presets", TabPresets.Draw, null, true),
            ("Automation", TabAutomation.Draw, null, true),
            ("Whitelist", TabWhitelist.Draw, null, true),
            ("Settings", TabSettings.Draw, null, true),
            (C.Debug?"Debugger":null, DrawDebugger, ImGuiColors.DalamudGrey, true),
            InternalLog.ImGuiTab(C.Debug),
            ]);
    }


    internal static uint Opcode = 0;
    internal static long Addr = 0;
    internal static int ID = 0;
    internal static float sa4 = -1;
    internal static int sa5 = 0;
    internal static int sa6 = 0;
    internal static int sa7 = 0;
    public static void DrawDebugger()
    {
        if(ImGui.CollapsingHeader("Apply SHE"))
        {
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("id", ref ID);
            ImGui.SetNextItemWidth(150f);
            ImGui.InputFloat("a4", ref sa4);
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("a5", ref sa5);
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("a6", ref sa6);
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("a7", ref sa7);
            if (ImGui.Button("Do"))
            {
                var addr = Svc.Targets.Target?.Address ?? Player.Object.Address;
                P.Memory.ApplyStatusHitEffectHook.Original((StatusHitEffectKind)ID, addr, addr, sa4, (byte)sa5, (short)sa6, (byte)sa7);
            }
            if (ImGui.Button("Do (all players)"))
            {
                foreach (var x in Svc.Objects)
                {
                    if (x is PlayerCharacter pc)
                    {
                        var addr = pc.Address;
                        P.Memory.ApplyStatusHitEffectHook.Original((StatusHitEffectKind)ID, addr, addr, sa4, (byte)sa5, (short)sa6, (byte)sa7);
                    }
                }
            }
            if (ImGui.BeginCombo("From StatusHitEffect", "From StatusHitEffect"))
            {
                foreach (var x in Svc.Data.GetExcelSheet<StatusHitEffect>())
                {
                    if (ImGui.Selectable($"{x.Location.Value.Location} / {(StatusHitEffectKind)x.Location.Row}", false, ImGuiSelectableFlags.DontClosePopups))
                    {
                        var addr = Svc.Targets.Target?.Address ?? Player.Object.Address;
                        P.Memory.ApplyStatusHitEffectHook.Original((StatusHitEffectKind)x.Location.Row, addr, addr, sa4, (byte)sa5, (short)sa6, (byte)sa7);
                    }
                }
                ImGui.EndCombo();
            }
            if (ImGui.BeginCombo("From ActionCastVFX", "From ActionCastVFX"))
            {
                foreach (var x in Svc.Data.GetExcelSheet<ActionCastVFX>())
                {
                    if (ImGui.Selectable($"{x.VFX.Value.Location} / {(StatusHitEffectKind)x.VFX.Row}", false, ImGuiSelectableFlags.DontClosePopups))
                    {
                        var addr = Svc.Targets.Target?.Address ?? Player.Object.Address;
                        P.Memory.ApplyStatusHitEffectHook.Original((StatusHitEffectKind)x.VFX.Row, addr, addr, sa4, (byte)sa5, (short)sa6, (byte)sa7);
                    }
                }
                ImGui.EndCombo();
            }
            if (ImGui.BeginCombo("From StatusLoopVFX", "From StatusLoopVFX"))
            {
                foreach (var x in Svc.Data.GetExcelSheet<StatusLoopVFX>())
                {
                    if (ImGui.Selectable($"{x.VFX.Value.Location} / {(StatusHitEffectKind)x.VFX.Row}", false, ImGuiSelectableFlags.DontClosePopups))
                    {
                        var addr = Svc.Targets.Target?.Address ?? Player.Object.Address;
                        P.Memory.ApplyStatusHitEffectHook.Original((StatusHitEffectKind)x.VFX.Row, addr, addr, sa4, (byte)sa5, (short)sa6, (byte)sa7);
                    }
                }
                ImGui.EndCombo();
            }
        }
        if (ImGui.CollapsingHeader("Actor control hook"))
        {
            ImGui.Checkbox($"Suppress", ref Suppress);
            ImGuiEx.InputUint($"dec", ref Opcode);
            ImGuiEx.InputHex($"hex", ref Opcode);
            if (ImGui.Button("Enable")) P.Memory.ProcessActorControlPacketHook.Enable();
            if (ImGui.Button("Pause")) P.Memory.ProcessActorControlPacketHook.Pause();
            if (ImGui.Button("Disable")) P.Memory.ProcessActorControlPacketHook.Disable();
            ImGuiEx.Text($"Enabled: {P.Memory.ProcessActorControlPacketHook.IsEnabled}");
            ImGuiEx.Text($"Created: {P.Memory.ProcessActorControlPacketHook.IsCreated}");
        }
        if (ImGui.CollapsingHeader("Packet hook"))
        {
            ImGui.Checkbox($"Suppress", ref Suppress);
            ImGuiEx.InputUint($"dec", ref Opcode);
            ImGuiEx.InputHex($"hex", ref Opcode);
            if (ImGui.Button("Enable")) P.Memory.PacketDispatcher_OnReceivePacketHook.Enable();
            if (ImGui.Button("Pause")) P.Memory.PacketDispatcher_OnReceivePacketHook.Pause();
            if (ImGui.Button("Disable")) P.Memory.PacketDispatcher_OnReceivePacketHook.Disable();
            ImGuiEx.Text($"Enabled: {P.Memory.PacketDispatcher_OnReceivePacketHook.IsEnabled}");
            ImGuiEx.Text($"Created: {P.Memory.PacketDispatcher_OnReceivePacketHook.IsCreated}");
        }
        if (ImGui.CollapsingHeader("Friendlist"))
        {
            ImGuiEx.Text(Utils.GetFriendlist().Print("\n"));
        }
        if (ImGui.CollapsingHeader("Mare players"))
        {
            if (P.IPCProcessor.GetMarePlayers.TryInvoke(out var list))
            {
                ImGuiEx.Text(list.Print("\n"));
            }
        }
        if (ImGui.CollapsingHeader("IPC"))
        {
            P.IPCTester.Draw();
        }
        if(ImGui.CollapsingHeader("Visible party"))
        {
            ImGuiEx.Text(P.CommonProcessor.PartyListProcessor.GetVisibleParty().Print("\n"));
        }
        if(ImGui.CollapsingHeader("Flytext debugger"))
        {
            if(ImGui.Button("Enable hook"))
            {
                P.Memory.UnkDelegateHook.Enable();
            }
            ImGui.SameLine();
            if(ImGui.Button("Disable hook"))
            {
                P.Memory.UnkDelegateHook.Disable();
            }
            ImGui.SameLine();
            ImGui.Checkbox($"Suppress", ref Suppress);
            if (ImGui.Button("Enable ac hook"))
            {
                P.Memory.ProcessActorControlPacketHook.Enable();
            }
            ImGui.SameLine();
            if (ImGui.Button("Disable ac hook"))
            {
                P.Memory.ProcessActorControlPacketHook.Disable();
            }
            if (ImGui.Button("Enable bl hook"))
            {
                P.Memory.BattleLog_AddToScreenLogWithScreenLogKindHook.Enable();
            }
            ImGui.SameLine();
            if (ImGui.Button("Disable bl hook"))
            {
                P.Memory.BattleLog_AddToScreenLogWithScreenLogKindHook.Disable();
            }

            if(ImGui.BeginCombo("object", $"{OID:X8}"))
            {
                foreach (var x in Svc.Objects.Where(x => x is PlayerCharacter).Cast<PlayerCharacter>())
                {
                    if (ImGui.Selectable($"{x.Name}")) OID = x.ObjectId;
                }
                ImGui.EndCombo();
            }
            ImGuiEx.EnumCombo("Message id", ref MessageID);
            ImGuiEx.InputUint("status id", ref StatusID);
            ImGuiEx.InputUint("a4", ref a4);
            ImGuiEx.InputUint("a5", ref a5);
            ImGuiEx.InputUint("a7", ref a7);
            ImGuiEx.InputUint("a8", ref a8);
            ImGui.Checkbox("From me", ref My);
            ImGui.Button("Execute");
            if (ImGui.IsItemHovered() && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Right)))
            {
                if(Svc.Objects.TryGetFirst(x => x.ObjectId == OID, out var obj))
                {
                    P.Memory.BattleLog_AddToScreenLogWithScreenLogKindHook.Original(obj.Address, My ? Player.Object.Address : obj.Address, MessageID, 5, (byte)a4, (int)a5, (int)StatusID, (int)a7, (int)a8);
                    Notify.Info($"Success");
                }
            }

        }
        ImGui.Checkbox("Enable UI modifications", ref C.Enabled);
        if (ImGui.CollapsingHeader("Status debugging"))
        {
            ImGuiEx.Text($"{P.CommonProcessor.HoveringOver:X16}");
            ImGuiEx.Text($"Statuses: {Player.Object.StatusList.Count(x => P.CommonProcessor.PositiveStatuses.Contains(x.StatusId))}|{Player.Object.StatusList.Count(x => P.CommonProcessor.NegativeStatuses.Contains(x.StatusId))}|{Player.Object.StatusList.Count(x => P.CommonProcessor.SpecialStatuses.Contains(x.StatusId))}");
            foreach(var x in Player.Object.StatusList)
            {
                if(x.StatusId != 0)
                {
                    ImGuiEx.Text($"{x.StatusId}, {x.GameData.Name}, permanent: {x.GameData.IsPermanent}, category: {x.GameData.StatusCategory}");
                }
            }
            if(Svc.Targets.Target is PlayerCharacter pc)
            {
                ImGuiEx.Text($"Target id: {(nint)ClientObjectManager.Instance()->GetObjectByIndex(16):X16}");
            }

            ImGuiEx.Text($"SeenPlayers:\n{P.SeenPlayers.Print("\n")}");
        }
        if (ImGui.BeginCombo("Select status manager", $"{Owner}"))
        {
            foreach(var x in C.StatusManagers)
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
        if (ImGui.Button("Target") && Svc.Targets.Target is PlayerCharacter pct)
        {
            Owner = pct.GetNameWithWorld();
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f);
        if (ImGui.BeginCombo("##Players around", "Players around"))
        {
            foreach (var x in Svc.Objects)
            {
                if (x is PlayerCharacter pc)
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
                if (x.GameObject is PlayerCharacter pc)
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
                        for (int i = 2; i < x.MaxStacks; i++)
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
                            ImGui.Image(t.ImGuiHandle, new(24, 32));
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
                    manager.AddOrUpdate(Status.JSONClone());
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
                    manager.AddOrUpdate(Status.JSONClone().PrepareToApply());
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt($"Random buffs", ref Cnt);
                ImGui.SameLine();
                if (ImGui.Button("add"))
                {
                    for (int i = 0; i < Cnt; i++)
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
                        var array = Svc.Objects.Where(x => x is PlayerCharacter pc && pc.IsTargetable).Cast<PlayerCharacter>().ToArray();
                        Utils.GetMyStatusManager(array[Random.Shared.Next(array.Length)]).AddOrUpdate(Status.JSONClone().PrepareToApply());
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
                    manager.Apply(a);
                }
                ImGui.Separator();
            }
            List<ImGuiEx.EzTableEntry> entries = [];
            foreach(var x in manager.Statuses)
            {
                entries.Add(new("", false, delegate
                {
                    if(ThreadLoadImageHandler.TryGetIconTextureWrap((uint)x.IconID, false, out var icon))
                    {
                        ImGui.Image(icon.ImGuiHandle, new Vector2(24, 32) * 0.75f);
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
            ImGuiEx.EzTable(null, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders, entries);
        }
    }

}
