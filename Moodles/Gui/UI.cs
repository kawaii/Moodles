using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Interface.Utility.Raii;
using ECommons.Configuration;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Moodles.Gui;

public static unsafe class UI
{
    public static bool Suppress = false;
    public static readonly Vector2 StatusIconSize = new(24, 32);
    private static uint OID = 0;
    private static FlyTextKind MessageID = FlyTextKind.Debuff;
    private static uint a4 = 0;
    private static uint a5 = 0;
    private static uint a7 = 0;
    private static uint a8 = 0;
    private static uint StatusID = 0;
    private static bool My = false;



    static int selected = 0;
    public static void Draw()
    {
        if (EzThrottler.Throttle("PeriodicConfigSave", 30 * 1000)) EzConfig.Save();

        var tabs = new List<(string Name, Action Draw)>
        {
            ("Moodles",    TabMoodles.Draw),
            ("Presets",    TabPresets.Draw),
            ("Automation", TabAutomation.Draw),
            ("Settings",   TabSettings.Draw),
        };
        if (C.FuckupTab2) tabs.Add(("Cleanup", TabFuckup.Draw));
        if (C.Debug) tabs.Add(("Debugger", DrawDebugger));
        if (C.Debug) tabs.Add(("Log", InternalLog.PrintImgui));

        ImEtheirys.ButtonSelectorStrip("library_filters_selector", new(ImEtheirys.GetRemainingWidth(), ImEtheirys.GetLineHeight()), ref selected, [.. tabs.Select(t => t.Name)]);

        if (selected >= 0 && selected < tabs.Count)
            tabs[selected].Draw();
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
        if (ImGui.CollapsingHeader("Apply SHE"))
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
                var addr = Svc.Targets.Target?.Address ?? LocalPlayer.Address;
                P.Memory.SpawnSHE((uint)ID, addr, addr, sa4, (char)sa5, (UInt16)sa6, (char)sa7);
            }
            if (ImGui.Button("Do (all players)"))
            {
                foreach (nint chara in CharaWatcher.Rendered)
                {
                    P.Memory.SpawnSHE((uint)ID, chara, chara, sa4, (char)sa5, (UInt16)sa6, (char)sa7);
                }
            }
        }
        if (ImGui.CollapsingHeader("Actor control hook"))
        {
            ImGui.Checkbox($"Suppress", ref Suppress);
            ImGuiEx.InputUint($"dec", ref Opcode);
            ImGuiEx.InputHex($"hex", ref Opcode);
            /*if (ImGui.Button("Enable")) P.Memory.ProcessActorControlPacketHook.Enable();
            if (ImGui.Button("Pause")) P.Memory.ProcessActorControlPacketHook.Pause();
            if (ImGui.Button("Disable")) P.Memory.ProcessActorControlPacketHook.Disable();
            ImGuiEx.Text($"Enabled: {P.Memory.ProcessActorControlPacketHook.IsEnabled}");
            ImGuiEx.Text($"Created: {P.Memory.ProcessActorControlPacketHook.IsCreated}");*/
        }
        if (ImGui.CollapsingHeader("Packet hook"))
        {
            ImGui.Checkbox($"Suppress", ref Suppress);
            ImGuiEx.InputUint($"dec", ref Opcode);
            ImGuiEx.InputHex($"hex", ref Opcode);
            /*if (ImGui.Button("Enable")) P.Memory.PacketDispatcher_OnReceivePacketHook.Enable();
            if (ImGui.Button("Pause")) P.Memory.PacketDispatcher_OnReceivePacketHook.Pause();
            if (ImGui.Button("Disable")) P.Memory.PacketDispatcher_OnReceivePacketHook.Disable();
            ImGuiEx.Text($"Enabled: {P.Memory.PacketDispatcher_OnReceivePacketHook.IsEnabled}");
            ImGuiEx.Text($"Created: {P.Memory.PacketDispatcher_OnReceivePacketHook.IsCreated}");*/
        }
        if (ImGui.CollapsingHeader("Friendlist"))
        {
            ImGuiEx.Text(Utils.GetFriendlist().Print("\n"));
        }
        if (ImGui.CollapsingHeader("IPC"))
        {
            P.IPCTester.Draw();
        }
        if (ImGui.CollapsingHeader("Visible party"))
        {
            ImGuiEx.Text(P.CommonProcessor.PartyListProcessor.GetVisibleParty().Print("\n"));
        }
        if (ImGui.CollapsingHeader("Flytext debugger"))
        {
            /*if(ImGui.Button("Enable hook"))
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
            }*/
            if (ImGui.Button("Enable bl hook"))
            {
                P.Memory.BattleLog_AddToScreenLogWithScreenLogKindHook.Enable();
            }
            ImGui.SameLine();
            if (ImGui.Button("Disable bl hook"))
            {
                P.Memory.BattleLog_AddToScreenLogWithScreenLogKindHook.Disable();
            }

            if (ImGui.BeginCombo("object", $"{OID:X8}"))
            {
                unsafe
                {
                    foreach (Character* chara in CharaWatcher.Rendered)
                    {
                        if (ImGui.Selectable($"{chara->NameString}")) OID = chara->OwnerId;
                    }
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
                if (CharaWatcher.TryGetFirst(x => x.OwnerId == OID, out var chara))
                {
                    P.Memory.BattleLog_AddToScreenLogWithScreenLogKindDetour(chara, My ? LocalPlayer.Address : chara, MessageID, 5, (byte)a4, (int)a5, (int)StatusID, (int)a7, (int)a8);
                    Notify.Info($"Success");
                }
            }

        }
        ImGui.Checkbox("Enable UI modifications", ref C.Enabled);

        if (ImGui.CollapsingHeader("Status debugging"))
        {
            ImGuiEx.Text($"{P.CommonProcessor.HoveringOver:X16}");
            ImGuiEx.Text($"Statuses: {LocalPlayer.StatusList.Count(x => P.CommonProcessor.PositiveStatuses.Contains(x.StatusId))}" +
                $"|{LocalPlayer.StatusList.Count(x => P.CommonProcessor.NegativeStatuses.Contains(x.StatusId))}" +
                $"|{LocalPlayer.StatusList.Count(x => P.CommonProcessor.SpecialStatuses.Contains(x.StatusId))}");

            foreach (var x in LocalPlayer.StatusList)
            {
                if (x.StatusId != 0)
                {
                    ImGuiEx.Text($"{x.StatusId}, {x.GameData.ValueNullable?.Name}, permanent: {x.GameData.ValueNullable?.IsPermanent}, category: {x.GameData.ValueNullable?.StatusCategory}");
                }
            }
            if (Svc.Targets.Target is IPlayerCharacter pc)
            {
                ImGuiEx.Text($"Target id: {(nint)ClientObjectManager.Instance()->GetObjectByIndex(16):X16}");
            }

            ImGuiEx.Text($"SeenPlayers:\n{P.SeenPlayers.Print("\n")}");
        }
    }

    private static unsafe void DrawIpcHandles(string tableId, IEnumerable<nint> players)
    {
        if (!players.Any()) return;

        using var _ = ImRaii.PushIndent();
        try
        {
            using (var t = ImRaii.Table($"##ipcPlayers-{tableId}", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV))
            {
                if (!t) return;
                ImGui.TableSetupColumn("Address");
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("ObjIdx");
                ImGui.TableSetupColumn("ObjKind");
                ImGui.TableSetupColumn("EntityId");
                ImGui.TableHeadersRow();

                foreach (Character* chara in players)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{(nint)chara:X}");
                    ImGui.TableNextColumn();
                    ImGui.Text(chara->NameString.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(chara->ObjectIndex.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(chara->ObjectKind.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(chara->EntityId.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Error drawing rendered charas: {ex}");
        }
    }

}
