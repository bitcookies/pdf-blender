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
    private const int CurrentSchemaVersion = 7;
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
                if (document is not null && document.SchemaVersion == CurrentSchemaVersion)
                    return NormalizeCurrent(document);
            }
        }
        catch (JsonException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        // Development data is intentionally reset when the stored schema is
        // from an earlier iteration. There are no released user configurations
        // to migrate yet.
        return CreateDefault();
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

    private static ConfigurationDocument CreateDefault()
    {
        var configurations = BuiltInConfigurations.All
            .Select(configuration => configuration with
            {
                IsBuiltIn = false,
                IsLocked = false
            })
            .ToList();
        return new ConfigurationDocument(
            CurrentSchemaVersion,
            configurations,
            [null, "builtin-medium", null, null, null],
            "builtin-medium");
    }

    private static ConfigurationDocument NormalizeCurrent(ConfigurationDocument document)
    {
        var uniqueUser = (document.UserConfigurations ?? [])
            .Where(configuration => !string.IsNullOrWhiteSpace(configuration.Id))
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
