using ECommons;
using ECommons.DalamudServices;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Moodles;

internal static class Utils
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