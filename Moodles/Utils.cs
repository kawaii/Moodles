using Dalamud.Game.Text.SeStringHandling;
using ECommons;
using ECommons.ChatMethods;
using ECommons.DalamudServices;
using ImGuiNET;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Moodles;

internal unsafe static partial class Utils
{
    static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
    {
        Converters =
        {
            new AbstractConverter<Moodle, IMoodle>()
        }
    };

    /// <param name="obj"></param>
    /// <returns>Deserialized copy of <paramref name="obj"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? JSONClone<T>(this T obj)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj), Settings);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUint(this Vector4 color)
    {
        return ImGui.ColorConvertFloat4ToU32(color);
    }

    // If you think im learning how this BS Kite made works... think again :bceStare2:
    public static SeString ParseBBSeString(string text, bool nullTerminator = true) => ParseBBSeString(text, out _, nullTerminator);
    public static SeString ParseBBSeString(string text, out string? error, bool nullTerminator = true)
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
                    if (r == 0 || Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.UIColor>().GetRowOrDefault(r) == null) goto ColorError;
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
                    if (r == 0 || Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.UIColor>().GetRowOrDefault(r) == null) goto ColorError;
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
        catch (Exception e)
        {
            error = "Error: please check syntax.";
            return new SeStringBuilder().AddText($"{error}\0").Build();
        }
    }

    [GeneratedRegex(@"(\[color=[0-9a-zA-Z]+\])|(\[\/color\])|(\[glow=[0-9a-zA-Z]+\])|(\[\/glow\])|(\[i\])|(\[\/i\])", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SplitRegex();
}

public class AbstractConverter<TReal, TAbstract>
    : JsonConverter where TReal : TAbstract
{
    public override Boolean CanConvert(Type objectType)
        => objectType == typeof(TAbstract);

    public override Object? ReadJson(JsonReader reader, Type type, Object? value, JsonSerializer jser)
        => jser.Deserialize<TReal>(reader);

    public override void WriteJson(JsonWriter writer, Object? value, JsonSerializer jser)
        => jser.Serialize(writer, value);
}