using Lumina.Excel.Sheets;
using Lumina.Excel;
using Moodles.Moodles.Services.Interfaces;
using System.Collections.Generic;
using Moodles.Moodles.Services.Structs;
using Dalamud.Utility;
using Dalamud.Interface.FontIdentifier;

namespace Moodles.Moodles.Services.Wrappers;

internal class SheetsWrapper : ISheets
{
    public List<string> VFXPaths { get; private set; } = ["Clear"];
    public uint[] IconIDs { get; private set; } = [];
    public ClassJob[] FilterableJobs => FilterableJobList.ToArray();

    readonly DalamudServices DalamudServices;

    readonly List<IPetSheetData> petSheetCache = new List<IPetSheetData>();

    readonly Dictionary<uint, uint> IconStackCounts = [];
    readonly HashSet<uint> NegativeStatuses = [];
    readonly HashSet<uint> PositiveStatuses = [];
    readonly HashSet<uint> SpecialStatuses = [];
    readonly HashSet<uint> DispelableIcons = [];

    readonly ExcelSheet<Companion>? petSheet;
    readonly ExcelSheet<Pet>? battlePetSheet;
    readonly ExcelSheet<World>? worlds;
    readonly ExcelSheet<Status>? statuses;
    readonly ExcelSheet<ClassJob>? classJobs;

    readonly List<ClassJob> FilterableJobList = new List<ClassJob>();

    public SheetsWrapper(DalamudServices dalamudServices)
    {
        DalamudServices = dalamudServices;

        petSheet = dalamudServices.DataManager.GetExcelSheet<Companion>();
        battlePetSheet = dalamudServices.DataManager.GetExcelSheet<Pet>();
        worlds = dalamudServices.DataManager.GetExcelSheet<World>();
        statuses = dalamudServices.DataManager.GetExcelSheet<Status>();
        classJobs = dalamudServices.DataManager.GetExcelSheet<ClassJob>();

        SetupPetSheetCache();
        SetupStatusCaches();
        SetupJobList();
    }

    void SetupJobList()
    {
        if (classJobs == null) return;

        foreach (ClassJob job in classJobs)
        {
            if (job.RowId == 0) continue;
            if (!job.ItemSoulCrystal.IsValid) continue;

            FilterableJobList.Add(job);
        }

        FilterableJobList.Sort((j1, j2) => j1.Role.CompareTo(j2.Role));
    }

    void SetupPetSheetCache()
    {
        SetupCompanions();
        SetupBattlePets();
    }

    void SetupStatusCaches()
    {
        if (statuses == null) return;

        List<uint> temporaryList = new List<uint>();

        foreach (Status status in statuses)
        {
            if (temporaryList.Contains(status.Icon)) continue;
            if (status.Icon == 0) continue;
            if (status.Name.ExtractText().IsNullOrEmpty()) continue;

            temporaryList.Add(status.Icon);
        }

        foreach (Status status in statuses)
        {
            if (status.HitEffect.ValueNullable == null) continue;
            if (status.HitEffect.Value.Location.ValueNullable == null) continue;

            string vfxPath = status.HitEffect.Value.Location.Value.Location.ExtractText();
            if (vfxPath.IsNullOrWhitespace()) continue;
            if (VFXPaths.Contains(vfxPath)) continue;

            VFXPaths.Add(vfxPath);
        }

        foreach (Status status in statuses)
        {
            if (IconStackCounts.TryGetValue(status.Icon, out uint count))
            {
                if (count < status.MaxStacks)
                {
                    IconStackCounts[status.Icon] = status.MaxStacks;
                }
            }
            else
            {
                IconStackCounts[status.Icon] = status.MaxStacks;
            }

            if (NegativeStatuses.Contains(status.RowId) || PositiveStatuses.Contains(status.RowId) || SpecialStatuses.Contains(status.RowId)) continue;

            if (status.CanIncreaseRewards == 1)
            {
                SpecialStatuses.Add(status.RowId);
            }
            else if (status.StatusCategory == 1)
            {
                PositiveStatuses.Add(status.RowId);
            }
            else if (status.StatusCategory == 2)
            {
                NegativeStatuses.Add(status.RowId);
                DispelableIcons.Add(status.Icon);
                for (var i = 1; i < status.MaxStacks; i++)
                {
                    DispelableIcons.Add((uint)(status.Icon + i));
                }
            }
        }
        
        IconIDs = temporaryList.ToArray();
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

    public bool StatusIsDispellable(uint statusId)
    {
        return DispelableIcons.Contains(statusId);
    }

    public uint? GetStackCount(uint iconId)
    {
        if (IconStackCounts.TryGetValue(iconId, out uint value))
        {
            return value;
        }

        return null;
    }

    public ClassJob? GetJob(uint id)
    {
        if (classJobs == null) return null;

        foreach (ClassJob job in classJobs)
        {
            if (job.RowId != id) continue;

            return job;
        }

        return null;
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

    public Status? GetStatusFromIconId(uint iconId)
    {
        if (statuses == null) return null;

        foreach (Status status in statuses)
        {
            if (status.Icon != iconId) continue;

            return status;
        }

        return null;
    }

    public Status? GetStatus(uint statusId)
    {
        if (statuses == null) return null;

        foreach (Status status in statuses)
        {
            if (status.RowId != statusId) continue;

            return status;
        }

        return null;
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
