using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PDFBlender.App;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public sealed record AppPreferences(
    ThemePreference Theme = ThemePreference.System,
    UiLanguage Language = UiLanguage.System,
    int SelectedBlendLevel = 1,
    bool RestorePresets = false,
    bool ToastSoundEnabled = true,
    bool AllowMultipleInstances = false,
    bool BannerNotificationsEnabled = true,
    bool WindowsNotificationsEnabled = false,
    string ToastSound = "toast-1.mp3");

internal static class AppPreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PDF Blender",
        "preferences.json");

    public static AppPreferences Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var preferences = JsonSerializer.Deserialize<AppPreferences>(
                    File.ReadAllText(FilePath), SerializerOptions);
                return Normalize(preferences ?? new AppPreferences());
            }
        }
        catch (JsonException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return new AppPreferences();
    }

    private static AppPreferences Normalize(AppPreferences preferences) => preferences with
    {
        Theme = Enum.IsDefined(preferences.Theme) ? preferences.Theme : ThemePreference.System,
        Language = Enum.IsDefined(preferences.Language) ? preferences.Language : UiLanguage.System,
        SelectedBlendLevel = Math.Clamp(preferences.SelectedBlendLevel, 0, 4),
        ToastSound = ToastSoundCatalog.Normalize(preferences.ToastSound)
    };

    public static void Save(AppPreferences preferences)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(preferences, SerializerOptions);
        var temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, FilePath, true);
    }
}
