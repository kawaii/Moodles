using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.ExcelServices;
using ECommons.PartyFunctions;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;
using System.Text.RegularExpressions;
using Status = Lumina.Excel.Sheets.Status;
using UIColor = ECommons.ChatMethods.UIColor;

namespace Moodles;
public static unsafe partial class Utils
{
    public static TargetApplyMode GetApplyMode()
    {
        if (Svc.Targets.Target is not IPlayerCharacter pc)
            return TargetApplyMode.NoTarget;
        if (IPC.GSpeakAvailable && IPC.GSpeakPlayerCache.ContainsKey(pc.Address))
            return TargetApplyMode.GSpeakPair;
        if (IPC.SundouleiaAvailable && IPC.SundouleiaPlayerCache.ContainsKey(pc.Address))
            return TargetApplyMode.Sundesmo;
        return TargetApplyMode.Local;
    }

    public static bool DurationSelector(string PermanentTitle, ref bool NoExpire, ref int Days, ref int Hours, ref int Minutes, ref int Seconds)
    {
        var modified = false;

        if(ImGui.Checkbox(PermanentTitle, ref NoExpire))
        {
            modified = true;
        }
        if(!NoExpire)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("D", ref Days, 0.1f, 0, 999);
            if(ImGui.IsItemDeactivatedAfterEdit()) modified = true;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("H##h", ref Hours, 0.1f, 0, 23);
            if(ImGui.IsItemDeactivatedAfterEdit()) modified = true;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("M##m", ref Minutes, 0.1f, 0, 59);
            if(ImGui.IsItemDeactivatedAfterEdit()) modified = true;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30);
            ImGui.DragInt("S##s", ref Seconds, 0.1f, 0, 59);
            if(ImGui.IsItemDeactivatedAfterEdit()) modified = true;

        }
        // Wait 5 seconds before firing our status modified event. (helps prevent flooding)
        if(modified) return true;
        // otherwise, return false for the change.
        return false;
    }

    public static bool CheckWhitelistGlobal(MyStatus status)
    {
        if(C.BroadcastAllowAll) return true;
        if(C.BroadcastAllowParty) return UniversalParty.Members.Any(x => x.Name == status.Applier);
        if(C.BroadcastAllowFriends) return GetFriendlist().Contains(status.Applier);
        return false;
    }

    public static List<string> GetFriendlist()
    {
        var ret = new List<string>();
        var friends = (InfoProxyFriendList*)InfoModule.Instance()->GetInfoProxyById(InfoProxyId.FriendList);
        for(var i = 0; i < friends->InfoProxyCommonList.CharDataSpan.Length; i++)
        {
            var entry = friends->InfoProxyCommonList.CharDataSpan[i];
            var name = entry.NameString;
            if(name != "")
            {
                ret.Add($"{name}@{ExcelWorldHelper.GetName(entry.HomeWorld)}");
            }
        }
        return ret;
    }

    public static bool IsNotNull(this MyStatus? status)
    {
        if(status == null) return false;
        if(status.Applier == null) return false;
        if(status.Description == null) return false;
        if(status.Title == null) return false;
        return true;
    }

    public static AtkResNode*[] GetNodeIconArray(AtkResNode* node, bool reverse = false)
    {
        var lst = new List<nint>();
        var atk = node->GetAsAtkComponentNode();
        if(atk is null) return [];
        var uldm = atk->Component->UldManager;
        for(var i = 0; i < uldm.NodeListCount; i++)
        {
            var next = uldm.NodeList[i];
            if(next == null) continue;
            if((int)next->Type < 1000) continue;
            if(((AtkUldComponentInfo*)next->GetAsAtkComponentNode()->Component->UldManager.Objects)->ComponentType == ComponentType.IconText)
            {
                lst.Add((nint)next);
            }
        }
        var ret = new AtkResNode*[lst.Count];
        for(var i = 0; i < lst.Count; i++)
        {
            ret[i] = (AtkResNode*)lst[reverse ? lst.Count - 1 - i : i];
        }
        return ret;
    }

    public static string PrintRange(this IEnumerable<string> s, out string FullList, string noneStr = "Any")
    {
        FullList = null!;
        var list = s.ToArray();
        if(list.Length == 0) return noneStr;
        if(list.Length == 1) return list[0].ToString();
        FullList = list.Select(x => x.ToString()).Join("\n");
        return $"{list.Length} selected";
    }

    public static string Censor(this string s, string censored)
    {
        return C.Censor ? censored : s;
    }

    public static string CensorCharacter(this string s)
    {
        return C.Censor ? s.Split(" ").Where(x => x.Length > 0).Select(x => $"{x[0]}.").Join(" ") : s;
    }

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
        return manager!;
    }

    public static long Time => DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public static ulong Frame => CSFramework.Instance()->FrameCounter;

    public static SeString ParseBBSeString(string text, bool nullTerminator = true) => ParseBBSeString(text, out _, nullTerminator);
    public static SeString ParseBBSeString(string text, out string error, bool nullTerminator = true)
    {
        try
        {
            error = null!;
            var result = SplitRegex().Split(text);
            var str = new SeStringBuilder();
            int[] valid = [0, 0, 0];
            foreach(var s in result)
            {
                if(s == string.Empty) continue;
                if(s.StartsWith("[color=", StringComparison.OrdinalIgnoreCase))
                {
                    var success = ushort.TryParse(s[7..^1], out var r);
                    if(!success)
                    {
                        r = (ushort)Enum.GetValues<UIColor>().FirstOrDefault(x => x.ToString().EqualsIgnoreCase(s[7..^1]));
                    }
                    if(r == 0 || Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.UIColor>().GetRowOrDefault(r) == null) goto ColorError;
                    str.AddUiForeground(r);
                    valid[0]++;
                }
                else if(s.Equals("[/color]", StringComparison.OrdinalIgnoreCase))
                {
                    str.AddUiForegroundOff();
                    if(valid[0] <= 0) goto ParseError;
                    valid[0]--;
                }
                else if(s.StartsWith("[glow=", StringComparison.OrdinalIgnoreCase))
                {
                    var success = ushort.TryParse(s[6..^1], out var r);
                    if(!success)
                    {
                        r = (ushort)Enum.GetValues<UIColor>().FirstOrDefault(x => x.ToString().EqualsIgnoreCase(s[6..^1]));
                    }
                    if(r == 0 || Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.UIColor>().GetRowOrDefault(r) == null) goto ColorError;
                    str.AddUiGlow(r);
                    valid[1]++;
                }
                else if(s.Equals("[/glow]", StringComparison.OrdinalIgnoreCase))
                {
                    str.AddUiGlowOff();
                    if(valid[1] <= 0) goto ParseError;
                    valid[1]--;
                }
                else if(s.Equals("[i]", StringComparison.OrdinalIgnoreCase))
                {
                    str.AddItalicsOn();
                    valid[2]++;
                }
                else if(s.Equals("[/i]", StringComparison.OrdinalIgnoreCase))
                {
                    str.AddItalicsOff();
                    if(valid[2] <= 0) goto ParseError;
                    valid[2]--;
                }
                else
                {
                    str.AddText(s);
                }
            }
            if(!valid.All(x => x == 0))
            {
                goto ParseError;
            }
            if(nullTerminator) str.AddText("\0");
            return str.Build();
        ParseError:
            error = "Error: Opening and closing elements mismatch.";
            return new SeStringBuilder().AddText($"{error}\0").Build();
        ColorError:
            error = "Error: Color is out of range.";
            return new SeStringBuilder().AddText($"{error}\0").Build();
        }
        catch(Exception)
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
            if(x.Icon == iconID) return x.RowId;
            if(x.MaxStacks > 1 && iconID >= x.Icon + 1 && iconID < x.Icon + x.MaxStacks) return x.RowId;
        }
        return 1;
    }

    public static string FindVFXPathByIconID(uint iconID)
    {
        foreach (var x in Svc.Data.GetExcelSheet<Status>())
        {
            if (x.Icon == iconID) return x.HitEffect.ValueNullable?.Location.ValueNullable?.Location.ExtractText() ?? string.Empty;
            if (x.MaxStacks > 1 && iconID >= x.Icon + 1 && iconID < x.Icon + x.MaxStacks) return x.HitEffect.ValueNullable?.Location.ValueNullable?.Location.ExtractText() ?? string.Empty;
        }
        return string.Empty;
    }

    public static string GetVfxPath(string path) => string.IsNullOrEmpty(path) ? "" : $"vfx/common/eff/{path}.avfx";

    public static MyStatus PrepareToApply(this MyStatus status, params PrepareOptions[] opts)
    {
        status = status.JSONClone();
        if(opts.Contains(PrepareOptions.ChangeGUID)) status.GUID = Guid.NewGuid();
        status.Persistent = opts.Contains(PrepareOptions.Persistent);
        if(status.NoExpire)
        {
            status.ExpiresAt = long.MaxValue;
        }
        else
        {
            status.ExpiresAt = Time + status.TotalDurationSeconds;
        }
        return status;
    }

    private static Dictionary<uint, IconInfo?> IconInfoCache = [];
    public static IconInfo? GetIconInfo(uint iconID)
    {
        if(IconInfoCache.TryGetValue(iconID, out var iconInfo))
        {
            return iconInfo;
        }
        else
        {
            if(!Svc.Data.GetExcelSheet<Status>().TryGetFirst(x => x.Icon == iconID, out var data))
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

    // This should be not needed anymore but who knows.
    public static void CleanupNulls()
    {
        for(var i = C.SavedStatuses.Count - 1; i >= 0; i--)
        {
            var item = C.SavedStatuses[i];
            if(item == null)
            {
                PluginLog.Warning($"Cleaning up corrupted stats {i}");
                C.SavedStatuses.RemoveAt(i);
            }
            else if(item.IconID < 200000)
            {
                item.IconID += 200000;
                PluginLog.Information($"Adjusting icon id for {item.Title}");
            }
        }
        for(var i = C.SavedPresets.Count - 1; i >= 0; i--)
        {
            var item = C.SavedPresets[i];
            if(item == null)
            {
                PluginLog.Warning($"Cleaning up corrupted presets {i}");
                C.SavedPresets.RemoveAt(i);
            }
        }
    }
}
