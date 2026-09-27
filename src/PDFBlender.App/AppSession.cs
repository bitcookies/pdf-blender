using System.IO;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

internal sealed class AppSession
{
    private readonly List<BlendConfiguration> _userConfigurations;
    private List<string?> _slotIds;
    private string? _selectedConfigurationId;

    public IReadOnlyList<BlendPreset> Presets { get; } = PDFBlender.Core.Models.Presets.All;
    public IReadOnlyList<BlendConfiguration> BuiltInConfigurations { get; } = PDFBlender.Core.Models.BuiltInConfigurations.All;
    public IReadOnlyList<BlendConfiguration> UserConfigurations => _userConfigurations;
    public IReadOnlyList<BlendConfiguration> AllConfigurations => BuiltInConfigurations.Concat(_userConfigurations).ToList();
    public IReadOnlyList<BlendConfiguration?> SlotConfigurations => _slotIds.Select(id => id is null ? null : GetConfiguration(id)).ToList();
    public IReadOnlyList<string?> SlotIds => _slotIds;
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
        _slotIds = NormalizeSlots(_slotIds);
        SelectedBlendLevel = Math.Clamp(Preferences.SelectedBlendLevel, 0, 4);
        EnsureSelectedSlot();
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
        var target = Math.Clamp(index, 0, 4);
        var selected = SlotConfigurations.ElementAtOrDefault(target);
        if (selected is null) return;
        SelectedBlendLevel = target;
        _selectedConfigurationId = selected.Id;
        Preferences = Preferences with { SelectedBlendLevel = SelectedBlendLevel };
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

    public bool RenameConfiguration(string id, string name)
    {
        var index = _userConfigurations.FindIndex(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || string.IsNullOrWhiteSpace(name)) return false;
        if (_userConfigurations[index].IsLocked) return false;
        _userConfigurations[index] = _userConfigurations[index] with { Name = name.Trim() };
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool UpdateConfiguration(string id, BlendSettings settings)
    {
        var index = _userConfigurations.FindIndex(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || _userConfigurations[index].IsLocked) return false;
        _userConfigurations[index] = _userConfigurations[index] with { Settings = settings };
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool DeleteConfiguration(string id)
    {
        var configuration = _userConfigurations.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        if (configuration is null || configuration.IsLocked) return false;
        var removed = _userConfigurations.RemoveAll(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed) return false;
        for (var index = 0; index < _slotIds.Count; index++)
            if (string.Equals(_slotIds[index], id, StringComparison.OrdinalIgnoreCase)) _slotIds[index] = null;
        if (string.Equals(_selectedConfigurationId, id, StringComparison.OrdinalIgnoreCase)) _selectedConfigurationId = SlotConfigurations.FirstOrDefault(configuration => configuration is not null)?.Id;
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool AddToSlots(string id, int? targetIndex = null)
    {
        if (GetConfiguration(id) is null) return false;
        var target = targetIndex ?? _slotIds.FindIndex(slot => slot is null);
        if (target < 0 || target >= 5) return false;
        var oldIndex = _slotIds.FindIndex(slot => string.Equals(slot, id, StringComparison.OrdinalIgnoreCase));
        if (oldIndex == target) return true;
        for (var index = 0; index < _slotIds.Count; index++)
            if (index != target && string.Equals(_slotIds[index], id, StringComparison.OrdinalIgnoreCase)) _slotIds[index] = null;
        _slotIds[target] = id;
        SelectedBlendLevel = target;
        _selectedConfigurationId = id;
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool RemoveFromSlots(string id)
    {
        var removed = false;
        for (var index = 0; index < _slotIds.Count; index++)
            if (string.Equals(_slotIds[index], id, StringComparison.OrdinalIgnoreCase)) { _slotIds[index] = null; removed = true; }
        if (!removed) return false;
        SaveAll();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool SetConfigurationLocked(string id, bool locked)
    {
        var index = _userConfigurations.FindIndex(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;
        _userConfigurations[index] = _userConfigurations[index] with { IsLocked = locked };
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
        _slotIds = NormalizeSlots(_slotIds);
        EnsureSelectedSlot();
        Preferences = Preferences with { SelectedBlendLevel = SelectedBlendLevel };
        SavePreferences();
        try { ConfigurationStore.Save(new ConfigurationDocument(2, _userConfigurations, _slotIds, _selectedConfigurationId)); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static List<string?> NormalizeSlots(IEnumerable<string?> slots)
    {
        var result = slots.Take(5).ToList();
        while (result.Count < 5) result.Add(null);
        return result;
    }

    private void EnsureSelectedSlot()
    {
        SelectedBlendLevel = Math.Clamp(SelectedBlendLevel, 0, 4);
        var current = SlotConfigurations.ElementAtOrDefault(SelectedBlendLevel);
        if (current is not null)
        {
            _selectedConfigurationId = current.Id;
            return;
        }

        for (var index = 0; index < SlotConfigurations.Count; index++)
        {
            var configuration = SlotConfigurations[index];
            if (configuration is null) continue;
            SelectedBlendLevel = index;
            _selectedConfigurationId = configuration.Id;
            return;
        }

        SelectedBlendLevel = 0;
        _selectedConfigurationId = null;
    }
}
