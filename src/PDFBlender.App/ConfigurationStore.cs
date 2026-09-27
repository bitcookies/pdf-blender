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
    private const int CurrentSchemaVersion = 3;
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
                if (document is not null && document.SchemaVersion is 1 or 2 or CurrentSchemaVersion)
                {
                    return Normalize(document with { SchemaVersion = CurrentSchemaVersion });
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
                Name = string.IsNullOrWhiteSpace(group.First().Name) ? "Custom" : group.First().Name.Trim()
            })
            .ToList();
        var validIds = new HashSet<string>(BuiltInConfigurations.All.Select(configuration => configuration.Id), StringComparer.OrdinalIgnoreCase);
        foreach (var configuration in uniqueUser) validIds.Add(configuration.Id);

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
