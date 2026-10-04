using System.IO;
using System.Security.Cryptography;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

internal sealed class AppSession
{
    private readonly List<BlendConfiguration> _userConfigurations;
    private List<string?> _slotIds;
    private string? _selectedConfigurationId;

    public IReadOnlyList<BlendConfiguration> BuiltInConfigurations { get; } = PDFBlender.Core.Models.BuiltInConfigurations.All;
    public IReadOnlyList<BlendConfiguration> AllConfigurations => _userConfigurations;
    public IReadOnlyList<BlendConfiguration?> SlotConfigurations => _slotIds.Select(id => id is null ? null : GetConfiguration(id)).ToList();
    public IReadOnlyList<string?> SlotIds => _slotIds;
    public AppPreferences Preferences { get; private set; }
    public int SelectedBlendLevel { get; private set; }
    public string? SelectedConfigurationId => _selectedConfigurationId;
    public BlendSettings Settings => SlotConfigurations.ElementAtOrDefault(SelectedBlendLevel)?.Settings ?? BlendSettings.Default;
    public string? InputPath { get; private set; }
    public IReadOnlyList<string> InputPaths { get; private set; } = [];

    public event EventHandler<SessionChangedEventArgs>? SessionChanged;
    public event EventHandler? Changed;

    public AppSession()
    {
        Preferences = AppPreferencesStore.Load();
        UiText.SetLanguage(Preferences.Language);
        var document = ConfigurationStore.Load();
        _userConfigurations = document.UserConfigurations ?? [];
        var seededInitialPresets = !ConfigurationStore.Exists && _userConfigurations.Count == 0;
        if (seededInitialPresets)
            SeedInitialPresets();
        var metadataChanged = EnsureConfigurationMetadata();
        _slotIds = document.SlotIds ?? [];
        _selectedConfigurationId = document.SelectedConfigurationId;
        _slotIds = NormalizeSlots(_slotIds);
        SelectedBlendLevel = Math.Clamp(Preferences.SelectedBlendLevel, 0, 4);
        EnsureSelectedSlot();
        if (seededInitialPresets || metadataChanged)
            SaveAll();
    }

    public BlendConfiguration? GetConfiguration(string id) =>
        _userConfigurations.FirstOrDefault(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));

    public void SetInputPath(string? path)
    {
        SetInputPaths(string.IsNullOrWhiteSpace(path) ? [] : [path]);
    }

    public void SetInputPaths(IEnumerable<string>? paths)
    {
        InputPaths = (paths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        InputPath = InputPaths.FirstOrDefault();
        RaiseChanged(SessionChangeKind.InputFiles);
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
        RaiseChanged(SessionChangeKind.BlendSelection);
    }

    public string AddUserConfiguration(string? name = null, BlendSettings? settings = null, string? colorKey = null, string? iconKey = null)
    {
        var id = "custom-" + Guid.NewGuid().ToString("N");
        var requestedName = string.IsNullOrWhiteSpace(name) ? UiText.Get("customConfiguration") : name.Trim();
        _userConfigurations.Add(new BlendConfiguration(id, MakeUniqueName(requestedName), false, settings ?? new BlendSettings
        {
            NormalizeContentWidth = true,
            Protection = ProtectionMethod.None,
            Watermark = WatermarkStyle.None,
            Noise = NoiseStrength.Off
        }, false, CreateShortId(), ConfigurationPalette.Normalize(colorKey ?? ConfigurationPalette.ForIndex(_userConfigurations.Count)), ConfigurationIconCatalog.Normalize(iconKey ?? ConfigurationIconCatalog.ForIndex(_userConfigurations.Count))));
        _selectedConfigurationId = id;
        SaveAll();
        RaiseChanged(SessionChangeKind.Configurations);
        return id;
    }

    public string AddPresetConfiguration(string presetId)
    {
        var preset = BuiltInConfigurations.FirstOrDefault(item => string.Equals(item.Id, presetId, StringComparison.OrdinalIgnoreCase));
        if (preset is null) return string.Empty;
        var id = "preset-" + Guid.NewGuid().ToString("N");
        _userConfigurations.Add(preset with { Id = id, Name = MakeUniqueName(LocalizedPresetName(preset)), IsBuiltIn = false, IsLocked = false, ShortId = CreateShortId(), ColorKey = ConfigurationPalette.ForIndex(_userConfigurations.Count), IconKey = ConfigurationIconCatalog.ForIndex(_userConfigurations.Count) });
        _selectedConfigurationId = id;
        SaveAll();
        RaiseChanged(SessionChangeKind.Configurations);
        return id;
    }

    public bool RenameConfiguration(string id, string name)
    {
        var index = _userConfigurations.FindIndex(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || string.IsNullOrWhiteSpace(name)) return false;
        if (_userConfigurations[index].IsLocked) return false;
        _userConfigurations[index] = _userConfigurations[index] with { Name = name.Trim() };
        SaveAll();
        RaiseChanged(SessionChangeKind.Configurations);
        return true;
    }

    public bool UpdateConfiguration(string id, BlendSettings settings, string? colorKey = null, string? iconKey = null)
    {
        var index = _userConfigurations.FindIndex(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || _userConfigurations[index].IsLocked) return false;
        _userConfigurations[index] = _userConfigurations[index] with { Settings = settings, ColorKey = ConfigurationPalette.Normalize(colorKey ?? _userConfigurations[index].ColorKey), IconKey = ConfigurationIconCatalog.Normalize(iconKey ?? _userConfigurations[index].IconKey) };
        SaveAll();
        RaiseChanged(SessionChangeKind.Configurations);
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
        RaiseChanged(SessionChangeKind.Configurations);
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
        RaiseChanged(SessionChangeKind.Configurations);
        return true;
    }

    public bool RemoveFromSlots(string id)
    {
        var removed = false;
        for (var index = 0; index < _slotIds.Count; index++)
            if (string.Equals(_slotIds[index], id, StringComparison.OrdinalIgnoreCase)) { _slotIds[index] = null; removed = true; }
        if (!removed) return false;
        SaveAll();
        RaiseChanged(SessionChangeKind.Configurations);
        return true;
    }

    public bool SetConfigurationLocked(string id, bool locked)
    {
        var index = _userConfigurations.FindIndex(configuration => string.Equals(configuration.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;
        _userConfigurations[index] = _userConfigurations[index] with { IsLocked = locked };
        SaveAll();
        RaiseChanged(SessionChangeKind.Configurations);
        return true;
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
        try { ConfigurationStore.Save(new ConfigurationDocument(6, _userConfigurations, _slotIds, _selectedConfigurationId)); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public void ApplyPreferences(ThemePreference? theme = null, UiLanguage? language = null, bool? toastSoundEnabled = null, bool? allowMultipleInstances = null, bool? bannerNotificationsEnabled = null, bool? windowsNotificationsEnabled = null, string? toastSound = null)
    {
        Preferences = Preferences with
        {
            Theme = theme ?? Preferences.Theme,
            Language = language ?? Preferences.Language,
            ToastSoundEnabled = toastSoundEnabled ?? Preferences.ToastSoundEnabled,
            AllowMultipleInstances = allowMultipleInstances ?? Preferences.AllowMultipleInstances,
            BannerNotificationsEnabled = bannerNotificationsEnabled ?? Preferences.BannerNotificationsEnabled,
            WindowsNotificationsEnabled = windowsNotificationsEnabled ?? Preferences.WindowsNotificationsEnabled,
            ToastSound = toastSound ?? Preferences.ToastSound
        };
        UiText.SetLanguage(Preferences.Language);
        SaveAll();
        RaiseChanged(SessionChangeKind.Preferences);
    }

    private void SeedInitialPresets()
    {
        foreach (var preset in BuiltInConfigurations)
        {
            if (_userConfigurations.Any(configuration => string.Equals(configuration.Id, preset.Id, StringComparison.OrdinalIgnoreCase))) continue;
            _userConfigurations.Add(preset with
            {
                Name = LocalizedPresetName(preset),
                IsBuiltIn = false,
                IsLocked = false
            });
        }
    }

    private bool EnsureConfigurationMetadata()
    {
        var changed = false;
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < _userConfigurations.Count; index++)
        {
            var configuration = _userConfigurations[index];
            var shortId = IsValidShortId(configuration.ShortId) && usedIds.Add(configuration.ShortId!)
                ? configuration.ShortId
                : CreateShortId(usedIds);
            var colorKey = ConfigurationPalette.Normalize(configuration.ColorKey);
            var iconKey = ConfigurationIconCatalog.Normalize(configuration.IconKey);
            if (!string.Equals(configuration.ShortId, shortId, StringComparison.Ordinal) || !string.Equals(configuration.ColorKey, colorKey, StringComparison.OrdinalIgnoreCase) || !string.Equals(configuration.IconKey, iconKey, StringComparison.Ordinal))
            {
                _userConfigurations[index] = configuration with { ShortId = shortId, ColorKey = colorKey, IconKey = iconKey };
                changed = true;
            }
        }
        return changed;
    }

    private string CreateShortId(ISet<string>? reserved = null)
    {
        reserved ??= _userConfigurations.Select(item => item.ShortId).Where(IsValidShortId).Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        do
        {
            var value = RandomNumberGenerator.GetInt32(0, 1679616);
            const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
            var chars = new char[4];
            for (var index = chars.Length - 1; index >= 0; index--)
            {
                chars[index] = alphabet[value % alphabet.Length];
                value /= alphabet.Length;
            }
            var candidate = new string(chars);
            if (reserved.Add(candidate)) return candidate;
        } while (true);
    }

    private static bool IsValidShortId(string? value) =>
        value is { Length: 4 } && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'z');
    private string MakeUniqueName(string requestedName)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedName) ? UiText.Get("customConfiguration") : requestedName.Trim();
        if (!_userConfigurations.Any(item => string.Equals(item.Name, baseName, StringComparison.CurrentCultureIgnoreCase))) return baseName;
        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseName} ({suffix})";
            if (!_userConfigurations.Any(item => string.Equals(item.Name, candidate, StringComparison.CurrentCultureIgnoreCase))) return candidate;
        }
    }

    private static string LocalizedPresetName(BlendConfiguration preset)
    {
        var name = preset.Id switch
        {
            "builtin-light" => UiText.Get("light"),
            "builtin-medium" => UiText.Get("medium"),
            "builtin-heavy" => UiText.Get("heavy"),
            _ => preset.Name
        };
        return $"{UiText.Get("presetPrefix")} {name}";
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

    private void RaiseChanged(SessionChangeKind kind)
    {
        SessionChanged?.Invoke(this, new SessionChangedEventArgs(kind));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
