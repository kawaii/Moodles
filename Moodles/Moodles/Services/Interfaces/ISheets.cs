using Lumina.Excel.Sheets;
using System.Collections.Generic;

namespace Moodles.Moodles.Services.Interfaces;

internal interface ISheets
{
    List<string> VFXPaths { get; }
    uint[] IconIDs { get; }
    ClassJob[] FilterableJobs { get; }

    IPetSheetData? GetPet(int skeletonID);

    bool IsValidBattlePet(int skeleton);

    string? GetWorldName(ushort worldID);

    ClassJob? GetJob(uint id);
    Status? GetStatusFromIconId(uint iconId);
    Status? GetStatus(uint statusId);

    uint? GetStackCount(uint iconId);

    bool StatusIsDispellable(uint statusId);
}
