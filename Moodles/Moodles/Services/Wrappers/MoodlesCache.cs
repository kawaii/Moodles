using Lumina.Excel.Sheets;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moodles.Moodles.Services.Wrappers;

internal class MoodlesCache : IMoodlesCache
{
    Dictionary<uint, IconInfo?> IconInfoCache = [];

    readonly DalamudServices DalamudServices;
    readonly ISheets Sheets;

    public MoodlesCache(DalamudServices dalamudServices, ISheets sheets)
    {
        DalamudServices = dalamudServices;
        Sheets = sheets;

        for (int i = 0; i < Sheets.IconIDs.Length; i++)
        {
            uint iconID = Sheets.IconIDs[i];
            _ = GetStatusIconInfo(iconID, false);
        }

        SortCache();
    }

    public IconInfo? GetStatusIconInfo(uint iconID, bool triggerSort = true)
    {
        if (IconInfoCache.TryGetValue(iconID, out IconInfo? info))
        {
            return info;
        }

        Status? foundStatus = Sheets.GetStatusFromIconId(iconID);
        if (foundStatus == null)
        {
            IconInfoCache[iconID] = null;
            return null;
        }

        IconInfo newInfo = new IconInfo
        (
            iconID,
            foundStatus.Value.Name.ExtractText(),
            foundStatus.Value.CanIncreaseRewards == 1 ? StatusType.Special : (foundStatus.Value.StatusCategory == 2 ? StatusType.Negative : StatusType.Positive),
            foundStatus.Value.MaxStacks > 1,
            foundStatus.Value.ClassJobCategory.Value,
            foundStatus.Value.IsFcBuff,
            foundStatus.Value.Description.ExtractText()
        );

        IconInfoCache[iconID] = newInfo;

        if (triggerSort)
        {
            SortCache();
        }

        return info;
    }

    void SortCache()
    {
        IconInfoCache = IconInfoCache.OrderBy(kvp => kvp.Value?.IconID ?? uint.MaxValue).ToDictionary();
    }
}
