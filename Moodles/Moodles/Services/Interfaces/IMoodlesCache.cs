using Moodles.Moodles.Services.Data;

namespace Moodles.Moodles.Services.Interfaces;

internal interface IMoodlesCache
{
    IconInfo? GetStatusIconInfo(uint iconID, bool triggerSort = true);
}
