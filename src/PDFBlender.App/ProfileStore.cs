using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

internal sealed record ProfileDefinition(string Id, BlendSettings Settings)
{
    // Optional per-level overrides keep older profiles readable while allowing
    // the Configuration page to edit STIR, BLEND, and PULSE independently.
    public Dictionary<string, BlendSettings> LevelSettings { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

internal static class ProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PDFBlender",
        "profiles.json");

    public static IReadOnlyList<ProfileDefinition> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var profiles = JsonSerializer.Deserialize<List<ProfileDefinition>>(
                    File.ReadAllText(FilePath), SerializerOptions);
                if (profiles is { Count: > 0 }) return profiles;
            }
        }
        catch (JsonException) { }
        catch (IOException) { }

        return CreateDefaults();
    }

    public static void Save(IEnumerable<ProfileDefinition> profiles)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(profiles, SerializerOptions);
        File.WriteAllText(FilePath, json);
    }

    private static IReadOnlyList<ProfileDefinition> CreateDefaults() =>
    [
        new("default", Presets.For(BlendMode.Blend)),
        new("research", Presets.For(BlendMode.Pulse)),
        new("public", Presets.For(BlendMode.Stir))
    ];
}
