using System.IO;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

/// <summary>
/// Shared in-process state for the shell and its pages.
/// </summary>
internal sealed class AppSession
{
    public IReadOnlyList<BlendPreset> Presets { get; } = PDFBlender.Core.Models.Presets.All;
    public List<ProfileDefinition> Configurations { get; } = ProfileStore.Load().ToList();
    public AppPreferences Preferences { get; private set; }
    public BlendSettings Settings { get; private set; }
    public int SelectedConfigurationIndex { get; private set; }
    public string? InputPath { get; private set; }
    public int SelectedBlendLevel => Math.Clamp(Preferences.SelectedBlendLevel, 0, Presets.Count - 1);

    public event EventHandler? Changed;

    public AppSession()
    {
        Preferences = AppPreferencesStore.Load();
        SelectedConfigurationIndex = 0;
        Settings = Configurations.Count > 0 ? Configurations[0].Settings : BlendSettings.Default;
        Settings = GetLevelSettings(SelectedBlendLevel);
    }

    public void SetInputPath(string? path)
    {
        InputPath = path;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetBlendLevel(int index)
    {
        var selected = Math.Clamp(index, 0, Presets.Count - 1);
        Settings = GetLevelSettings(selected);
        Preferences = Preferences with { SelectedBlendLevel = selected };
        SavePreferences();
        SaveSelectedConfiguration();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SelectConfiguration(int index)
    {
        if (index < 0 || index >= Configurations.Count)
        {
            return;
        }

        SelectedConfigurationIndex = index;
        Settings = GetLevelSettings(SelectedBlendLevel);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ApplySettings(BlendSettings settings, bool notify = true)
    {
        Settings = settings;
        SaveSelectedConfiguration();
        if (notify)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ApplyPreferences(ThemePreference? theme = null, UiLanguage? language = null)
    {
        Preferences = Preferences with
        {
            Theme = theme ?? Preferences.Theme,
            Language = language ?? Preferences.Language
        };
        SavePreferences();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SavePreferences()
    {
        try { AppPreferencesStore.Save(Preferences); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void SaveSelectedConfiguration()
    {
        if (SelectedConfigurationIndex < 0 || SelectedConfigurationIndex >= Configurations.Count)
        {
            return;
        }

        var profile = Configurations[SelectedConfigurationIndex];
        var levels = new Dictionary<string, BlendSettings>(profile.LevelSettings, StringComparer.OrdinalIgnoreCase)
        {
            [Presets[SelectedBlendLevel].Mode.ToString()] = Settings
        };
        Configurations[SelectedConfigurationIndex] = profile with { Settings = Settings, LevelSettings = levels };
        try { ProfileStore.Save(Configurations); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private BlendSettings GetLevelSettings(int index)
    {
        if (Configurations.Count == 0)
        {
            return Presets[index].Settings;
        }

        var profile = Configurations[Math.Clamp(SelectedConfigurationIndex, 0, Configurations.Count - 1)];
        return profile.LevelSettings.TryGetValue(Presets[index].Mode.ToString(), out var overrideSettings)
            ? overrideSettings
            : profile.Settings;
    }
}
