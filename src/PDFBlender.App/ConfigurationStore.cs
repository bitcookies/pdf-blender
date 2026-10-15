using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

internal sealed record ConfigurationDocument(
    int SchemaVersion,
    List<BlendConfiguration> UserConfigurations,
    List<string?> SlotIds,
    string? SelectedConfigurationId);

internal static class ConfigurationStore
{
    internal const int CurrentSchemaVersion = 12;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static bool Exists => File.Exists(FilePath);

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PDF Blender",
        "configurations.json");

    public static ConfigurationDocument Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var document = JsonSerializer.Deserialize<ConfigurationDocument>(File.ReadAllText(FilePath), SerializerOptions);
                if (document is not null)
                {
                    if (document.SchemaVersion == CurrentSchemaVersion)
                        return NormalizeCurrent(document);
                    if (document.SchemaVersion == 11)
                    {
                        var migrated = MigrateV11(document);
                        Save(migrated);
                        return migrated;
                    }
                    BackupUnreadableConfiguration();
                }
            }
        }
        catch (JsonException) { BackupUnreadableConfiguration(); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        // Unknown or malformed documents fall back to safe defaults without overwriting the original file.
        return CreateDefault();
    }

    private static void BackupUnreadableConfiguration()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Copy(FilePath, FilePath + ".invalid.bak", true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static void Save(ConfigurationDocument document)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(NormalizeCurrent(document), SerializerOptions);
        var temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, FilePath, true);
    }

    private static ConfigurationDocument MigrateV11(ConfigurationDocument document)
    {
        var migratedUsers = (document.UserConfigurations ?? [])
            .Select(configuration => configuration with
            {
                Settings = (configuration.Settings ?? BlendSettings.Default) with
                {
                    CopyLayer = configuration.Settings?.CopyLayer is CopyLayerMode.ReplaceWithGibberish
                        ? CopyLayerMode.UnicodeScramble
                        : configuration.Settings?.CopyLayer ?? CopyLayerMode.Preserve
                }
            })
            .ToList();
        return NormalizeCurrent(new ConfigurationDocument(CurrentSchemaVersion, migratedUsers, document.SlotIds ?? [], document.SelectedConfigurationId));
    }

    private static ConfigurationDocument CreateDefault()
    {
        return new ConfigurationDocument(CurrentSchemaVersion, [], [null, null, null, null, null], null);
    }

    private static ConfigurationDocument NormalizeCurrent(ConfigurationDocument document)
    {
        var uniqueUser = (document.UserConfigurations ?? [])
            .Where(configuration => !string.IsNullOrWhiteSpace(configuration.Id))
            .Where(configuration => !BuiltInConfigurations.IsReservedId(configuration.Id))
            .GroupBy(configuration => configuration.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First() with
            {
                IsBuiltIn = false,
                Name = string.IsNullOrWhiteSpace(group.First().Name) ? "Custom" : group.First().Name.Trim(),
                Settings = group.First().Settings ?? BlendSettings.Default
            })
            .ToList();

        var validIds = new HashSet<string>(
            uniqueUser.Select(configuration => configuration.Id),
            StringComparer.OrdinalIgnoreCase);
        var seenSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var slots = (document.SlotIds ?? [])
            .Take(5)
            .Select(id => id is not null && validIds.Contains(id) && seenSlots.Add(id) ? id : null)
            .ToList();
        while (slots.Count < 5) slots.Add(null);

        var selected = document.SelectedConfigurationId is not null && validIds.Contains(document.SelectedConfigurationId)
            ? document.SelectedConfigurationId
            : slots.FirstOrDefault();
        return new ConfigurationDocument(CurrentSchemaVersion, uniqueUser, slots, selected);
    }
}
