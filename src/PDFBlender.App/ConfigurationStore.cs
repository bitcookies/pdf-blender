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
    private const int CurrentSchemaVersion = 5;
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
                if (document is not null && document.SchemaVersion is >= 1 and <= CurrentSchemaVersion)
                {
                    var normalized = Normalize(document);
                    // Persist legacy repairs immediately. This makes the schema-3
                    // empty-slot migration durable even before the user changes a
                    // setting or opens the configuration page.
                    if (document.SchemaVersion != CurrentSchemaVersion || !(document.SlotIds ?? []).SequenceEqual(normalized.SlotIds))
                    {
                        try
                        {
                            Save(normalized);
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }
                    return normalized;
                }
            }
        }
        catch (JsonException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return new ConfigurationDocument(CurrentSchemaVersion, [], [null, "builtin-medium", null, null, null], "builtin-medium");
    }

    public static void Save(ConfigurationDocument document)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(Normalize(document), SerializerOptions);
        var temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, FilePath, true);
    }

    private static ConfigurationDocument Normalize(ConfigurationDocument document)
    {
        var user = document.UserConfigurations ?? [];
        var uniqueUser = user
            .Where(configuration => !string.IsNullOrWhiteSpace(configuration.Id))
            .GroupBy(configuration => configuration.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First() with
            {
                IsBuiltIn = false,
                Name = string.IsNullOrWhiteSpace(group.First().Name) ? "Custom" : group.First().Name.Trim(),
                Settings = group.First().Settings ?? BlendSettings.Default
            })
            .ToList();
        // Built-in presets are persisted in UserConfigurations after first use. Do not
        // keep stale slot references to a preset that is no longer present in the file.
        var validIds = new HashSet<string>(uniqueUser.Select(configuration => configuration.Id), StringComparer.OrdinalIgnoreCase);

        var seenSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var slots = (document.SlotIds ?? [])
            .Take(5)
            .Select(id => id is not null && validIds.Contains(id) && seenSlots.Add(id) ? id : null)
            .ToList();
        while (slots.Count < 5) slots.Add(null);

        // Version 3 could persist the configurations while losing every slot id.
        // Repair that one-time migration using the stable light/medium/heavy order;
        // later saves use schema 4, so an intentional empty slot remains empty.
        if (document.SchemaVersion < CurrentSchemaVersion && slots.All(id => id is null))
        {
            var preferredIds = new[] { "builtin-light", "builtin-medium", "builtin-heavy" };
            var migrationIds = preferredIds
                .Concat(uniqueUser.Select(configuration => configuration.Id))
                .Where(validIds.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();
            for (var index = 0; index < migrationIds.Length; index++) slots[index] = migrationIds[index];
        }
        var selected = document.SelectedConfigurationId is not null && validIds.Contains(document.SelectedConfigurationId)
            ? document.SelectedConfigurationId
            : slots.FirstOrDefault();
        return new ConfigurationDocument(CurrentSchemaVersion, uniqueUser, slots, selected);
    }
}
