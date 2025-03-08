using Lumina.Excel.Sheets;
using Lumina.Excel;
using Moodles.Moodles.Services.Interfaces;
using System.Collections.Generic;
using Moodles.Moodles.Services.Structs;

namespace Moodles.Moodles.Services.Wrappers;

internal class SheetsWrapper : ISheets
{
    readonly DalamudServices DalamudServices;

    readonly List<IPetSheetData> petSheetCache = new List<IPetSheetData>();

    readonly ExcelSheet<Companion>? petSheet;
    readonly ExcelSheet<Pet>? battlePetSheet;
    readonly ExcelSheet<World>? worlds;

    public SheetsWrapper(DalamudServices dalamudServices)
    {
        DalamudServices = dalamudServices;

        petSheet = dalamudServices.DataManager.GetExcelSheet<Companion>();
        battlePetSheet = dalamudServices.DataManager.GetExcelSheet<Pet>();
        worlds = dalamudServices.DataManager.GetExcelSheet<World>();

        SetupPetSheetCache();
    }

    void SetupPetSheetCache()
    {
        SetupCompanions();
        SetupBattlePets();
    }

    void SetupCompanions()
    {
        if (petSheet == null) return;

        foreach (Companion companion in petSheet)
        {
            if (!companion.Model.IsValid) continue;

            ModelChara? model = companion.Model.ValueNullable;
            if (model == null) continue;

            int modelID = (int)model.Value.RowId;

            petSheetCache.Add(new PetSheetData(modelID));
        }
    }

    void SetupBattlePets()
    {
        if (battlePetSheet == null) return;

        foreach (Pet pet in battlePetSheet)
        {
            uint sheetSkeleton = pet.RowId;

            if (!battlePetRemap.TryGetValue(sheetSkeleton, out int skeleton)) continue;

            petSheetCache.Add(new PetSheetData(skeleton));
        }
    }

    public IPetSheetData? GetPet(int skeletonID)
    {
        for (int i = 0; i < petSheetCache.Count; i++)
        {
            if (petSheetCache[i].Model == skeletonID)
            {
                return petSheetCache[i];
            }
        }

        return null;
    }

    public string? GetWorldName(ushort worldID)
    {
        if (worlds == null) return null;

        World? world = worlds.GetRow(worldID);
        if (world == null) return null;

        return world.Value.InternalName.ExtractText();
    }

    public bool IsValidBattlePet(int skeleton) => battlePetRemap.ContainsValue(skeleton);

    public readonly Dictionary<uint, int> battlePetRemap = new Dictionary<uint, int>()
    {
        { 6,    PluginConstants.Eos                     }, // EOS
        { 7,    PluginConstants.Selene                  }, // Selene

        { 1,    PluginConstants.EmeraldCarbuncle        }, // Emerald Carbuncle
        { 38,   PluginConstants.RubyCarbuncle           }, // Ruby Carbuncle
        { 2,    PluginConstants.TopazCarbuncle          }, // Topaz Carbuncle
        { 36,   PluginConstants.Carbuncle               }, // Carbuncle

        { 27,   PluginConstants.IfritEgi                }, // Ifrit-Egi
        { 28,   PluginConstants.TitanEgi                }, // Titan-Egi
        { 29,   PluginConstants.GarudaEgi               }, // Garuda-Egi 

        { 8,    PluginConstants.RookAutoTurret          }, // Rook Autoturret MCHN
        { 21,   PluginConstants.Seraph                  }, // Seraph
        { 18,   PluginConstants.AutomatonQueen          }, // Automaton Queen
        { 17,   PluginConstants.LivingShadow            }, // Esteem DRK

        { 14,   PluginConstants.Phoenix                 }, // Demi-Phoenix
        { 10,   PluginConstants.Bahamut                 }, // Demi-Bahamut
        { 32,   PluginConstants.GarudaII                }, // Emerald-Garuda
        { 31,   PluginConstants.TitanII                 }, // Topaz-Titan
        { 30,   PluginConstants.IffritII                }, // Ruby-Iffrit
        { 46,   PluginConstants.SolarBahamut            }, // Solar Bahamut
    };
}
