using System.IO;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

/// <summary>
/// Shared in-process state for the shell and its pages.
/// </summary>
internal sealed class AppSession
{
    public IReadOnlyList<BlendPreset> Presets { get; } = PDFBlender.Core.Models.Presets.All;
    public List<ProfileDefinition> Profiles { get; } = ProfileStore.Load().ToList();
    public AppPreferences Preferences { get; private set; }
    public BlendSettings Settings { get; private set; }
    public int SelectedProfileIndex { get; private set; }
    public string? InputPath { get; private set; }

    public event EventHandler? Changed;

    public AppSession()
    {
        Preferences = AppPreferencesStore.Load();
        SelectedProfileIndex = 0;
        Settings = Profiles.Count > 0 ? Profiles[0].Settings : BlendSettings.Default;
        var selected = Math.Clamp(Preferences.SelectedBlendLevel, 0, Presets.Count - 1);
        Settings = Presets[selected].Settings;
    }

    public void SetInputPath(string? path)
    {
        InputPath = path;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetBlendLevel(int index)
    {
        var selected = Math.Clamp(index, 0, Presets.Count - 1);
        Settings = Presets[selected].Settings;
        Preferences = Preferences with { SelectedBlendLevel = selected };
        SavePreferences();
        SaveSelectedProfile();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SelectProfile(int index)
    {
        if (index < 0 || index >= Profiles.Count)
        {
            return;
        }

        SelectedProfileIndex = index;
        Settings = Profiles[index].Settings;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ApplySettings(BlendSettings settings, bool notify = true)
    {
        Settings = settings;
        SaveSelectedProfile();
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

    private void SaveSelectedProfile()
    {
        if (SelectedProfileIndex < 0 || SelectedProfileIndex >= Profiles.Count)
        {
            return;
        }

        Profiles[SelectedProfileIndex] = Profiles[SelectedProfileIndex] with { Settings = Settings };
        try { ProfileStore.Save(Profiles); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
