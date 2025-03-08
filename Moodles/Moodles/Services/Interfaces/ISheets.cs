namespace Moodles.Moodles.Services.Interfaces;

internal interface ISheets
{
    IPetSheetData? GetPet(int skeletonID);

    bool IsValidBattlePet(int skeleton);

    string? GetWorldName(ushort worldID);
}
