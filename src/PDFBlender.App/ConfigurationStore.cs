using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

internal sealed record ConfigurationDocument(
    int SchemaVersion,
    List<BlendConfiguration> UserConfigurations,
    List<string> SlotIds,
    string? SelectedConfigurationId);

internal static class ConfigurationStore
{
    private const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

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
                {
                    return Normalize(document);
                }
            }
        }
        catch (JsonException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return new ConfigurationDocument(CurrentSchemaVersion, [], ["builtin-medium"], "builtin-medium");
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
            .Where(configuration => !configuration.IsBuiltIn && !string.IsNullOrWhiteSpace(configuration.Id))
            .GroupBy(configuration => configuration.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First() with { Name = string.IsNullOrWhiteSpace(group.First().Name) ? "Custom" : group.First().Name.Trim() })
            .ToList();
        var validIds = new HashSet<string>(BuiltInConfigurations.All.Select(configuration => configuration.Id), StringComparer.OrdinalIgnoreCase);
        foreach (var configuration in uniqueUser) validIds.Add(configuration.Id);

        var slots = (document.SlotIds ?? [])
            .Where(id => validIds.Contains(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
        var selected = document.SelectedConfigurationId is not null && validIds.Contains(document.SelectedConfigurationId)
            ? document.SelectedConfigurationId
            : slots.FirstOrDefault();
        return new ConfigurationDocument(CurrentSchemaVersion, uniqueUser, slots, selected);
    }
}
