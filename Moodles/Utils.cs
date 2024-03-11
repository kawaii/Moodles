using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons.ExcelServices;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using ECommons.PartyFunctions;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;
using System.Text.RegularExpressions;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using Status = Lumina.Excel.GeneratedSheets.Status;
using UIColor = ECommons.ChatMethods.UIColor;

namespace Moodles;
public static unsafe partial class Utils
{
    public static void SendMareMessage(this Preset Preset, PlayerCharacter target)
    {
        foreach (var s in C.SavedStatuses.Where(x => Preset.Statuses.Contains(x.GUID)))
        {
            var list = new List<MyStatus>();
            var preparedStatus = s.PrepareToApply();
            preparedStatus.Applier = Player.NameWithWorld ?? "";
            if (!preparedStatus.IsValid(out var error))
            {
                PluginLog.Error($"Could not apply status: {error}");
            }
            else
            {
                list.Add(preparedStatus);
            }
            if (list.Count > 0)
            {
                var message = new IncomingMessage(Player.NameWithWorld, target.GetNameWithWorld(), list);
                if (P.IPCProcessor.BroadcastMareMessage.TryInvoke(Convert.ToBase64String(message.Serialize())))
                {
                    Notify.Info($"Broadcast success");
                }
                else
                {
                    Notify.Error("Broadcast failed");
                }
            }
        }
    }

    public static void SendMareMessage(this MyStatus Status, PlayerCharacter target)
    {
        var preparedStatus = Status.PrepareToApply();
        preparedStatus.Applier = Player.NameWithWorld ?? "";
        if (!preparedStatus.IsValid(out var error))
        {
            Notify.Error($"Could not apply status: {error}");
        }
        else
        {
            var message = new IncomingMessage(Player.NameWithWorld, target.GetNameWithWorld(), [preparedStatus]);
            if (P.IPCProcessor.BroadcastMareMessage.TryInvoke(Convert.ToBase64String(message.Serialize())))
            {
                Notify.Info($"Broadcast success");
            }
            else
            {
                Notify.Error("Broadcast failed");
            }
        }
    }

    public static void DurationSelector(string PermanentTitle, ref bool NoExpire, ref int Days, ref int Hours, ref int Minutes, ref int Seconds)
    {
        ImGui.Checkbox(PermanentTitle, ref NoExpire);
        if (!NoExpire)
        {
            ImGui.SameLine();
            ImGuiEx.SetNextItemWidthScaled(30);
            ImGui.DragInt("D", ref Days, 0.1f, 0, 999);
            ImGui.SameLine();
            ImGuiEx.SetNextItemWidthScaled(30);
            ImGui.DragInt("H##h", ref Hours, 0.1f, 0, 23);
            ImGui.SameLine();
            ImGuiEx.SetNextItemWidthScaled(30);
            ImGui.DragInt("M##m", ref Minutes, 0.1f, 0, 59);
            ImGui.SameLine();
            ImGuiEx.SetNextItemWidthScaled(30);
            ImGui.DragInt("S##s", ref Seconds, 0.1f, 0, 59);
        }
    }

    public static bool CheckWhitelistGlobal(MyStatus status)
    {
        if (status.ExpiresAt > C.BroadcastDefaultEntry.MaxExpirationUnixTimeSeconds) return false;
        if (C.BroadcastAllowAll) return true;
        if (C.BroadcastAllowParty) return UniversalParty.Members.Any(x => x.Name == status.Applier);
        if (C.BroadcastAllowFriends) return GetFriendlist().Contains(status.Applier);
        return false;
    }

    public static List<string> GetFriendlist()
    {
        var ret = new List<string>();
        var friends = (InfoProxyFriendList*)InfoModule.Instance()->GetInfoProxyById(InfoProxyId.FriendList);
        for (int i = 0; i < friends->InfoProxyCommonList.CharDataSpan.Length; i++)
        {
            var entry = friends->InfoProxyCommonList.CharDataSpan[i];
            var name = MemoryHelper.ReadStringNullTerminated((nint)entry.Name);
            if(name != "")
            {
                ret.Add($"{name}@{ExcelWorldHelper.GetName(entry.HomeWorld)}");
            }
        }
        return ret;
    }

    static List<nint> MarePlayers = [];
    static ulong MarePlayersUpdated = 0;
    public static List<nint> GetMarePlayers()
    {
        if (Frame != MarePlayersUpdated)
        {
            MarePlayersUpdated = Frame;
            if (P.IPCProcessor.GetMarePlayers.TryInvoke(out var ret))
            {
                MarePlayers = ret;
            }
        }
        return MarePlayers;
    }

    public static bool IsNotNull(this MyStatus status)
    {
        if (status == null) return false;
        if (status.Applier == null) return false;
        if (status.Description == null) return false;
        if (status.Title == null) return false;
        return true;
    }

    public static AtkResNode*[] GetNodeIconArray(AtkResNode* node, bool reverse = false)
    {
        var lst = new List<nint>();
        var uldm = node->GetAsAtkComponentNode()->Component->UldManager;
        for (int i = 0; i < uldm.NodeListCount; i++)
        {
            var next = uldm.NodeList[i];
            if (next == null) continue;
            if ((int)next->Type < 1000) continue;
            if (((AtkUldComponentInfo*)next->GetAsAtkComponentNode()->Component->UldManager.Objects)->ComponentType == ComponentType.IconText)
            {
                lst.Add((nint)next);
            }
        }
        var ret = new AtkResNode*[lst.Count];
        for (int i = 0; i < lst.Count; i++)
        {
            ret[i] = (AtkResNode*)lst[reverse ? lst.Count - 1 - i : i];
        }
        return ret;
    }

    public static string PrintRange(this IEnumerable<string> s, out string FullList, string noneStr = "Any")
    {
        FullList = null;
        var list = s.ToArray();
        if (list.Length == 0) return noneStr;
        if (list.Length == 1) return list[0].ToString();
        FullList = list.Select(x => x.ToString()).Join("\n");
        return $"{list.Length} selected";
    }

    public static string Censor(this string s, string censored)
    {
        return C.Censor ? censored : s;
    }

    public static string CensorCharacter(this string s)
    {
        return C.Censor ? s.Split(" ").Where(x => x.Length > 0).Select(x => $"{x[0]}.").Join(" "):s;
    }

    public static IEnumerable<AutomationCombo> GetSuitableAutomation(PlayerCharacter pc = null)
    {
        pc ??= Player.Object;
        foreach (var x in C.AutomationProfiles)
        {
            if (x.Enabled && x.Character == pc.Name.ToString() && (x.World == 0 || x.World == pc.HomeWorld.Id))
            {
                foreach (var c in x.Combos)
                {
                    if (c.Jobs.Count == 0 || c.Jobs.Contains(pc.GetJob()))
                    {
                        yield return c;
                    }
                }
            }
        }
    }

    public static bool CanSpawnVfxFlytext(PlayerCharacter target)
    {
        if (!target.IsTargetable) return false;
        if (!Player.Interactable) return false;
        if (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
            || Svc.Condition[ConditionFlag.WatchingCutscene]
            || Svc.Condition[ConditionFlag.WatchingCutscene78]
            || Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
            || Svc.Condition[ConditionFlag.Occupied]
            || Svc.Condition[ConditionFlag.Occupied30]
            || Svc.Condition[ConditionFlag.Occupied33]
            || Svc.Condition[ConditionFlag.Occupied38]
            || Svc.Condition[ConditionFlag.Occupied39]
            || Svc.Condition[ConditionFlag.OccupiedInEvent]
            || Svc.Condition[ConditionFlag.BetweenAreas]
            || Svc.Condition[ConditionFlag.BetweenAreas51]
            || Svc.Condition[ConditionFlag.DutyRecorderPlayback]
            || Svc.Condition[ConditionFlag.LoggingOut]) return false;
        return true;
    }

    public static long Time => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static MyStatusManager GetMyStatusManager(string playerName, bool create = true)
    {
        if (!C.StatusManagers.TryGetValue(playerName, out var manager))
        {
            if (create)
            {
                PluginLog.Verbose($"Creating new status manager for {playerName}");
                manager = new();
                C.StatusManagers[playerName] = manager;
            }
        }
        return manager;
    }

    public static MyStatusManager GetMyStatusManager(this PlayerCharacter pc, bool create = true) => GetMyStatusManager(pc.GetNameWithWorld(), create);

    public static ulong Frame => CSFramework.Instance()->FrameCounter;

    public static SeString ParseBBSeString(string text, bool nullTerminator = true) => ParseBBSeString(text, out _, nullTerminator);
    public static SeString ParseBBSeString(string text, out string error, bool nullTerminator = true)
    {
        try
        {
            error = null;
            var result = SplitRegex().Split(text);
            var str = new SeStringBuilder();
            int[] valid = [0, 0, 0];
            foreach (var s in result)
            {
                if (s == string.Empty) continue;
                if (s.StartsWith("[color=", StringComparison.OrdinalIgnoreCase))
                {
                    var success = ushort.TryParse(s[7..^1], out var r);
                    if (!success)
                    {
                        r = (ushort)Enum.GetValues<UIColor>().FirstOrDefault(x => x.ToString().EqualsIgnoreCase(s[7..^1]));
                    }
                    if (r == 0 || Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.UIColor>().GetRow(r) == null) goto ColorError;
                    str.AddUiForeground(r);
                    valid[0]++;
                }
                else if (s.Equals("[/color]", StringComparison.OrdinalIgnoreCase))
                {
                    str.AddUiForegroundOff();
                    if (valid[0] <= 0) goto ParseError;
                    valid[0]--;
                }
                else if (s.StartsWith("[glow=", StringComparison.OrdinalIgnoreCase))
                {
                    var success = ushort.TryParse(s[6..^1], out var r);
                    if (!success)
                    {
                        r = (ushort)Enum.GetValues<UIColor>().FirstOrDefault(x => x.ToString().EqualsIgnoreCase(s[6..^1]));
                    }
                    if (r == 0 || Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.UIColor>().GetRow(r) == null) goto ColorError;
                    str.AddUiGlow(r);
                    valid[1]++;
                }
                else if (s.Equals("[/glow]", StringComparison.OrdinalIgnoreCase))
                {
                    str.AddUiGlowOff();
                    if (valid[1] <= 0) goto ParseError;
                    valid[1]--;
                }
                else if (s.Equals("[i]", StringComparison.OrdinalIgnoreCase))
                {
                    str.AddItalicsOn();
                    valid[2]++;
                }
                else if (s.Equals("[/i]", StringComparison.OrdinalIgnoreCase))
                {
                    str.AddItalicsOff();
                    if (valid[2] <= 0) goto ParseError;
                    valid[2]--;
                }
                else
                {
                    str.AddText(s);
                }
            }
            if (!valid.All(x => x == 0))
            {
                goto ParseError;
            }
            if (nullTerminator) str.AddText("\0");
            return str.Build();
        ParseError:
            error = "Error: Opening and closing elements mismatch.";
            return new SeStringBuilder().AddText($"{error}\0").Build();
        ColorError:
            error = "Error: Color is out of range.";
            return new SeStringBuilder().AddText($"{error}\0").Build();
        }
        catch(Exception e)
        {
            error = "Error: please check syntax.";
            return new SeStringBuilder().AddText($"{error}\0").Build();
        }
    }

    [GeneratedRegex(@"(\[color=[0-9a-zA-Z]+\])|(\[\/color\])|(\[glow=[0-9a-zA-Z]+\])|(\[\/glow\])|(\[i\])|(\[\/i\])", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SplitRegex();

    public static uint FindStatusByIconID(uint iconID)
    {
        foreach(var x in Svc.Data.GetExcelSheet<Status>())
        {
            if (x.Icon == iconID) return x.RowId;
            if (x.MaxStacks > 1 && iconID >= x.Icon + 1 && iconID < x.Icon + x.MaxStacks) return x.RowId;
        }
        return 1;
    }

    public static MyStatus PrepareToApply(this MyStatus status, params PrepareOptions[] opts)
    {
        status = status.JSONClone();
        if (opts.Contains(PrepareOptions.ChangeGUID)) status.GUID = Guid.NewGuid();
        status.Persistent = opts.Contains(PrepareOptions.Persistent);
        if (status.NoExpire)
        {
            status.ExpiresAt = long.MaxValue;
        }
        else
        {
            status.ExpiresAt = Time + status.TotalDurationSeconds;
        }
        return status;
    }

    static Dictionary<uint, IconInfo?> IconInfoCache = [];
    public static IconInfo? GetIconInfo(uint iconID)
    {
        if(IconInfoCache.TryGetValue(iconID, out var iconInfo))
        {
            return iconInfo;
        }
        else
        {
            var data = Svc.Data.GetExcelSheet<Status>().FirstOrDefault(x => x.Icon == iconID);
            if (data == null)
            {
                IconInfoCache[iconID] = null;
                return null;
            }
            var info = new IconInfo()
            {
                Name = data.Name.ExtractText(),
                IconID = iconID,
                Type = data.CanIncreaseRewards == 1 ? StatusType.Special : (data.StatusCategory == 2 ? StatusType.Negative : StatusType.Positive),
                ClassJobCategory = data.ClassJobCategory.Value,
                IsFCBuff = data.IsFcBuff,
                IsStackable = data.MaxStacks > 1,
                Description = data.Description.ExtractText(),

            };
            IconInfoCache[iconID] = info;
            return info;
        }
    }
}
