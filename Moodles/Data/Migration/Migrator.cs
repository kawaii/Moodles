using ECommons.Configuration;
using ECommons.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace Moodles.Data;

/// <summary>
///     An extension of the DefaultSerializationFactory to properly update 
///     outdated config and status structures.
///     
///     This migrator can work for existing and future Config & Status structure,
///     allowing you to manipulate the structure of any container when desired.
/// </summary>
public class MoodleSerializationFactory : DefaultSerializationFactory
{
    public static void BackupOldConfigs()
    {
        var configPath = EzConfig.DefaultConfigurationFileName;
        if (!File.Exists(configPath))
            return;

        // Backup the files if they used legacy configs to have in the case things break a bit.
        try
        {
            var j = JObject.Parse(File.ReadAllText(configPath, Encoding.UTF8));
            if (j["Version"] != null)
            {
                PluginLog.Information($"No backup needed for configs, a valid version is detected).");
            }
            else
            {
                var configDir = EzConfig.GetPluginConfigDirectory();
                var backupFolder = Path.Combine(EzConfig.GetPluginConfigDirectory(), "MigrationBackup");
                Directory.CreateDirectory(backupFolder);
                foreach (var file in Directory.GetFiles(configDir, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var dest = Path.Combine(backupFolder, Path.GetFileName(file));
                        if (!File.Exists(dest)) File.Copy(file, dest);
                    }
                    catch (Exception copyEx)
                    {
                        PluginLog.Error($"Failed to copy '{file}' to backup: {copyEx}");
                    }
                }
            }
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Failed to parse file: {ex}");
        }
    }

    public override T Deserialize<T>(string inputData)
    {
        // Get the deserializer settings.
        var type = typeof(T).GetFieldPropertyUnion("JsonSerializerSettings", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        JsonSerializerSettings settings;
        if (type != null && type.GetValue(null) is JsonSerializerSettings s)
        {
            settings = s;
            PluginLog.Verbose($"Using JSON serializer settings from object to perform deserialization");
        }
        else
        {
            settings = new JsonSerializerSettings()
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
            };
        }

        // If the type supports migration, use the migratable path
        if (IsMigratableObject(typeof(T)))
            return DeserializeMigratable<T>(inputData, settings);

        return JsonConvert.DeserializeObject<T>(inputData, settings) ?? throw new InvalidOperationException($"Deserialization of type {typeof(T).FullName} resulted in null value.");
    }

    // If the converted type is migratable.
    private bool IsMigratableObject(Type type) => type == typeof(Config) || type == typeof(MyStatus);

    private T DeserializeMigratable<T>(string inputData, JsonSerializerSettings settings)
    {
        var jObj = JObject.Parse(inputData);
        var version = jObj["Version"]?.Value<int>() ?? 1;
        // Check based on the type.
        if (typeof(T) == typeof(Config))
        {
            if (version < 2)
            {
                jObj = MigrateConfigToV2(jObj);
                PluginLog.Information("Migrated Config to V2.");
            }
        }
        else if (typeof(T) == typeof(MyStatus))
        {
            if (version < 2)
            {
                MigrateStatusToV2(jObj);
                PluginLog.Information($"Migrating Status to V2");
            }
        }
        // Deserialize directly from the new JObject into T
        return jObj.ToObject<T>(JsonSerializer.Create(settings))
           ?? throw new InvalidOperationException($"Failed to deserialization {typeof(T).FullName}. (Resulted in null!)");

    }

    private static JObject MigrateConfigToV2(JObject old)
    {
        old["Version"] = 2;
        // Migrate StatusManagers
        if (old["StatusManagers"] is JObject managers)
        {
            foreach (var manager in managers.Properties())
            {
                if (manager.Value["Statuses"] is JArray statuses)
                {
                    foreach (JObject statusObj in statuses)
                        MigrateStatusToV2(statusObj);
                }
            }
        }
        // Migrate SavedStatuses
        if (old["SavedStatuses"] is JArray saved)
        {
            foreach (JObject statusObj in saved)
                MigrateStatusToV2(statusObj);
        }
        return old;
    }

    private static void MigrateStatusToV2(JObject jObj)
    {
        // Old status info
        bool wasDispellable = jObj.Value<bool?>("Dispelable") ?? false;
        Guid statusOnDispel = Guid.TryParse(jObj.Value<string>("StatusOnDispell"), out var id) ? id : Guid.Empty;
        bool stacksIncreased = jObj.Value<bool?>("StackOnReapply") ?? false;
        int stackSteps = jObj.Value<int?>("StacksIncOnReapply") ?? (stacksIncreased ? 1 : 0);
        bool stacksTransferred = jObj.Value<bool?>("TransferStacksOnDispell") ?? false;

        // New Modifiers
        Modifiers modifiers = Modifiers.None;
        if (wasDispellable) modifiers |= Modifiers.CanDispel;
        if (stacksIncreased) modifiers |= Modifiers.StacksIncrease;
        if (stacksTransferred) modifiers |= Modifiers.StacksMoveToChain;

        // Add new fields
        jObj["StackSteps"] = stackSteps;
        jObj["Modifiers"] = (uint)modifiers;
        jObj["ChainedStatus"] = statusOnDispel.ToString();
        jObj["ChainTrigger"] = wasDispellable && statusOnDispel != Guid.Empty;

        // Remove old fields
        jObj.Remove("StatusOnDispell");
        jObj.Remove("StackOnReapply");
        jObj.Remove("TransferStacksOnDispell");
        jObj.Remove("Dispelable");
    }
}

