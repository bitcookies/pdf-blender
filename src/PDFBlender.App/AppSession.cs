using System.IO;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

internal sealed class AppSession
{
    private readonly List<BlendConfiguration> _userConfigurations;
    private List<string> _slotIds;
    private string? _selectedConfigurationId;

    public IReadOnlyList<BlendPreset> Presets { get; } = PDFBlender.Core.Models.Presets.All;
    public IReadOnlyList<BlendConfiguration> BuiltInConfigurations { get; } = PDFBlender.Core.Models.BuiltInConfigurations.All;
    public IReadOnlyList<BlendConfiguration> UserConfigurations => _userConfigurations;
    public IReadOnlyList<BlendConfiguration> AllConfigurations => BuiltInConfigurations.Concat(_userConfigurations).ToList();
    public IReadOnlyList<BlendConfiguration> SlotConfigurations => _slotIds
        .Select(GetConfiguration)
        .Where(configuration => configuration is not null)
        .Cast<BlendConfiguration>()
        .OrderBy(configuration => configuration.Intensity)
        .ThenBy(configuration => _slotIds.IndexOf(configuration.Id))
        .ToList();
    public IReadOnlyList<string> SlotIds => _slotIds;
    public AppPreferences Preferences { get; private set; }
    public int SelectedBlendLevel { get; private set; }
    public string? SelectedConfigurationId => _selectedConfigurationId;
    public BlendSettings Settings => SlotConfigurations.ElementAtOrDefault(SelectedBlendLevel)?.Settings ?? BlendSettings.Default;
    public string? InputPath { get; private set; }

    public event EventHandler? Changed;

    public AppSession()
    {
        Preferences = AppPreferencesStore.Load();
        var document = ConfigurationStore.Load();
        _userConfigurations = document.UserConfigurations ?? [];
        _slotIds = document.SlotIds ?? [];
        _selectedConfigurationId = document.SelectedConfigurationId;
        SelectedBlendLevel = Math.Clamp(Preferences.SelectedBlendLevel, 0, Math.Max(0, _slotIds.Count - 1));
    }

    public BlendConfiguration? GetConfiguration(string id) =>
        BuiltInConfigurations.Concat(_userConfigurations).FirstOrDefault(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));

    public void SetInputPath(string? path)
    {
        InputPath = path;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetBlendLevel(int index)
    {
        SelectedBlendLevel = _slotIds.Count == 0 ? 0 : Math.Clamp(index, 0, _slotIds.Count - 1);
        var selected = SlotConfigurations.ElementAtOrDefault(SelectedBlendLevel);
        _selectedConfigurationId = selected?.Id;
        Preferences = Preferences with { SelectedBlendLevel = SelectedBlendLevel };
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SelectConfiguration(string? id)
    {
        if (id is null || GetConfiguration(id) is null) return;
        _selectedConfigurationId = id;
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public string AddUserConfiguration(string? name = null, BlendSettings? settings = null)
    {
        var id = "custom-" + Guid.NewGuid().ToString("N");
        _userConfigurations.Add(new BlendConfiguration(id, string.IsNullOrWhiteSpace(name) ? "Custom" : name.Trim(), false, settings ?? new BlendSettings
        {
            NormalizeContentWidth = true,
            Protection = ProtectionMethod.None,
            Watermark = WatermarkStyle.None,
            Noise = NoiseStrength.Off
        }));
        _selectedConfigurationId = id;
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return id;
    }

    public string DuplicateConfiguration(string id)
    {
        var source = GetConfiguration(id) ?? throw new ArgumentException("Configuration was not found.", nameof(id));
        return AddUserConfiguration(source.Name + " Copy", source.Settings);
    }

    public bool RenameConfiguration(string id, string name)
    {
        var index = _userConfigurations.FindIndex(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || string.IsNullOrWhiteSpace(name)) return false;
        _userConfigurations[index] = _userConfigurations[index] with { Name = name.Trim() };
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool UpdateConfiguration(string id, BlendSettings settings)
    {
        var index = _userConfigurations.FindIndex(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;
        _userConfigurations[index] = _userConfigurations[index] with { Settings = settings };
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool DeleteConfiguration(string id)
    {
        var removed = _userConfigurations.RemoveAll(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed) return false;
        _slotIds.RemoveAll(slotId => string.Equals(slotId, id, StringComparison.OrdinalIgnoreCase));
        if (string.Equals(_selectedConfigurationId, id, StringComparison.OrdinalIgnoreCase)) _selectedConfigurationId = SlotConfigurations.FirstOrDefault()?.Id;
        SelectedBlendLevel = Math.Clamp(SelectedBlendLevel, 0, Math.Max(0, _slotIds.Count - 1));
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool AddToSlots(string id)
    {
        if (GetConfiguration(id) is null || _slotIds.Count >= 5 || _slotIds.Contains(id, StringComparer.OrdinalIgnoreCase)) return false;
        _slotIds.Add(id);
        SelectConfiguration(id);
        return true;
    }

    public bool RemoveFromSlots(string id)
    {
        var removed = _slotIds.RemoveAll(slotId => string.Equals(slotId, id, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed) return false;
        SelectedBlendLevel = Math.Clamp(SelectedBlendLevel, 0, Math.Max(0, _slotIds.Count - 1));
        _selectedConfigurationId = SlotConfigurations.ElementAtOrDefault(SelectedBlendLevel)?.Id;
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void ApplyPreferences(ThemePreference? theme = null, UiLanguage? language = null)
    {
        Preferences = Preferences with { Theme = theme ?? Preferences.Theme, Language = language ?? Preferences.Language };
        SavePreferences();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SavePreferences()
    {
        try { AppPreferencesStore.Save(Preferences); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void SaveAll()
    {
        SavePreferences();
        _slotIds = SlotConfigurations.Select(configuration => configuration.Id).ToList();
        SelectedBlendLevel = Math.Clamp(SelectedBlendLevel, 0, Math.Max(0, _slotIds.Count - 1));
        if (GetConfiguration(_selectedConfigurationId ?? string.Empty) is null)
            _selectedConfigurationId = SlotConfigurations.ElementAtOrDefault(SelectedBlendLevel)?.Id;
        try { ConfigurationStore.Save(new ConfigurationDocument(1, _userConfigurations, _slotIds, _selectedConfigurationId)); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
