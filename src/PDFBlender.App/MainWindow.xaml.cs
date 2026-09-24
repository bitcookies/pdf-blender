using Microsoft.Win32;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PDFBlender.Pdf;
using System.IO;
using System.Windows;

namespace PDFBlender.App;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<BlendPreset> _presets = Presets.All;
    private readonly List<ProfileDefinition> _profiles = ProfileStore.Load().ToList();
    private AppPreferences _preferences;
    private BlendSettings _settings = BlendSettings.Default;
    private string? _inputPath;
    private UiLanguage _language;
    private ThemePreference _themePreference;
    private bool _initializing;
    private CancellationTokenSource? _processingCts;
    private readonly IBlendProcessor _processor = new PdfBlendProcessor();

    public MainWindow()
    {
        _preferences = AppPreferencesStore.Load();
        _language = _preferences.Language;
        _themePreference = _preferences.Theme;

        InitializeComponent();
        InitializeSelectors();
        ApplyTheme();
        ApplyLanguage();
        SetInputPath(null);
    }

    private void InitializeSelectors()
    {
        _initializing = true;
        foreach (var profile in _profiles)
        {
            ProfileCombo.Items.Add(ProfileDisplayName(profile.Id));
        }

        ProfileCombo.SelectedIndex = 0;
        BlendLevelRailControl.SetLevels(BuildLevelItems());
        BlendLevelRailControl.SelectedIndex = Math.Clamp(
            _preferences.SelectedBlendLevel, 0, Math.Max(0, _presets.Count - 1));
        _selectedBlendLevel = BlendLevelRailControl.SelectedIndex;
        _settings = _presets[_selectedBlendLevel].Settings;
        _initializing = false;
    }

    private int _selectedBlendLevel;

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            CheckFileExists = true,
            Multiselect = false,
            Title = UiText.Get("browse")
        };

        if (dialog.ShowDialog(this) == true)
        {
            SetInputPath(dialog.FileName);
        }
    }

    private void DropTarget_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasPdfDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropTarget_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            var pdf = files.FirstOrDefault(path =>
                string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
            if (pdf is not null)
            {
                SetInputPath(pdf);
            }
        }

        e.Handled = true;
    }

    private async void BlendButton_Click(object sender, RoutedEventArgs e)
    {
        if (_inputPath is null)
        {
            StatusText.Text = UiText.Get("ready");
            return;
        }

        if (_processingCts is not null)
        {
            return;
        }

        var outputPath = OutputText.Text;
        if (string.IsNullOrWhiteSpace(outputPath) || outputPath == "—")
        {
            StatusText.Text = UiText.Get("outputInvalid");
            return;
        }

        var settings = _settings;
        _processingCts = new CancellationTokenSource();
        SetProcessingState(true);
        var progress = new Progress<BlendProgress>(value =>
        {
            ProcessingProgress.Value = value.Fraction * 100;
            StatusText.Text = $"{value.Stage} {value.CompletedPages}/{value.TotalPages}";
        });

        try
        {
            var result = await _processor.ProcessAsync(
                new BlendRequest(_inputPath, outputPath, settings), progress, _processingCts.Token);
            StatusText.Text = $"{UiText.Get("completed")}: {Path.GetFileName(result.OutputPath)}";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = UiText.Get("cancelled");
        }
        catch (Exception exception)
        {
            StatusText.Text = $"{UiText.Get("processingError")}: {exception.Message}";
        }
        finally
        {
            _processingCts.Dispose();
            _processingCts = null;
            SetProcessingState(false);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => _processingCts?.Cancel();

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings, _preferences) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _settings = dialog.Settings;
            _preferences = dialog.Preferences;
            _language = _preferences.Language;
            _themePreference = _preferences.Theme;
            SavePreferences();

            if (ProfileCombo.SelectedIndex >= 0 && ProfileCombo.SelectedIndex < _profiles.Count)
            {
                _profiles[ProfileCombo.SelectedIndex] = _profiles[ProfileCombo.SelectedIndex] with { Settings = _settings };
                try { ProfileStore.Save(_profiles); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            ApplyTheme();
            ApplyLanguage();
            StatusText.Text = UiText.Get("settingsSaved");
        }
    }

    private void ProfileCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_initializing || ProfileCombo.SelectedIndex < 0 || ProfileCombo.SelectedIndex >= _profiles.Count)
        {
            return;
        }

        _settings = _profiles[ProfileCombo.SelectedIndex].Settings;
        var matchingPreset = _presets
            .Select((preset, index) => (preset, index))
            .FirstOrDefault(pair => pair.preset.Settings == _settings);
        if (matchingPreset.preset is not null)
        {
            _initializing = true;
            BlendLevelRailControl.SelectedIndex = matchingPreset.index;
            _selectedBlendLevel = matchingPreset.index;
            _initializing = false;
        }

        StatusText.Text = $"{UiText.Get("profile")}: {ProfileDisplayName(_profiles[ProfileCombo.SelectedIndex].Id)}";
    }

    private void BlendLevelRailControl_SelectionChanged(object? sender, BlendLevelChangedEventArgs e)
    {
        if (_initializing || e.SelectedIndex < 0 || e.SelectedIndex >= _presets.Count)
        {
            return;
        }

        _selectedBlendLevel = e.SelectedIndex;
        _settings = _presets[e.SelectedIndex].Settings;
        _preferences = _preferences with { SelectedBlendLevel = _selectedBlendLevel };
        SavePreferences();
        StatusText.Text = e.Item.Summary;
    }

    private void ApplyLanguage()
    {
        UiText.SetLanguage(_language);
        SubtitleText.Text = UiText.Get("subtitle");
        DropTitleText.Text = UiText.Get("dropTitle");
        DropHintText.Text = UiText.Get("dropHint");
        LocalProcessingText.Text = UiText.Get("localProcessing");
        BrowseButtonText.Text = UiText.Get("browse");
        ProfileLabel.Text = UiText.Get("profile");
        OutputLabel.Text = UiText.Get("output");
        BlendButtonText.Text = UiText.Get("blend");
        CancelButtonText.Text = UiText.Get("cancel");
        CancelButton.ToolTip = UiText.Get("cancel");
        SettingsButton.ToolTip = UiText.Get("settings");
        ReadyText.Text = _inputPath is null ? UiText.Get("ready") : UiText.Get("selected");
        StatusText.Text = _inputPath is null ? UiText.Get("ready") : Path.GetFileName(_inputPath);
        BlendLevelRailControl.SetCaption(UiText.Get("blendLevel"));
        BlendLevelRailControl.SetAccessibilityName(UiText.Get("blendLevel"));
        BlendLevelRailControl.SetLevels(BuildLevelItems());
        BlendLevelRailControl.SelectedIndex = _selectedBlendLevel;
        UpdateProfileLabels();
    }

    private IReadOnlyList<BlendLevelItem> BuildLevelItems() =>
    [
        new(_presets[0].DisplayName, UiText.Get("stirSummary")),
        new(_presets[1].DisplayName, UiText.Get("blendSummary")),
        new(_presets[2].DisplayName, UiText.Get("pulseSummary"))
    ];

    private void UpdateProfileLabels()
    {
        var selectedIndex = Math.Max(ProfileCombo.SelectedIndex, 0);
        ProfileCombo.Items.Clear();
        foreach (var profile in _profiles)
        {
            ProfileCombo.Items.Add(ProfileDisplayName(profile.Id));
        }

        if (ProfileCombo.Items.Count > 0)
        {
            ProfileCombo.SelectedIndex = Math.Min(selectedIndex, ProfileCombo.Items.Count - 1);
        }
    }

    private static string ProfileDisplayName(string id) => id switch
    {
        "research" => UiText.Get("profileResearch"),
        "public" => UiText.Get("profilePublic"),
        _ => UiText.Get("profileDefault")
    };

    private void ApplyTheme() => ThemeManager.Apply(this, _themePreference);

    private void SetInputPath(string? path)
    {
        _inputPath = path;
        var hasFile = path is not null;
        ReadyText.Text = hasFile ? UiText.Get("selected") : UiText.Get("ready");
        FileNameText.Text = hasFile ? Path.GetFileName(path) : string.Empty;
        BlendButton.IsEnabled = hasFile;
        OutputText.Text = hasFile
            ? Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(path)}_blended.pdf")
            : "—";
        StatusText.Text = hasFile ? Path.GetFileName(path) : UiText.Get("ready");
    }

    private void SetProcessingState(bool processing)
    {
        BlendButton.IsEnabled = !processing && _inputPath is not null;
        BrowseButton.IsEnabled = !processing;
        ProfileCombo.IsEnabled = !processing;
        SettingsButton.IsEnabled = !processing;
        BlendLevelRailControl.IsEnabled = !processing;
        ProcessingProgress.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
        if (!processing)
        {
            ProcessingProgress.Value = 0;
        }
    }

    private void SavePreferences()
    {
        try { AppPreferencesStore.Save(_preferences); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static bool HasPdfDrop(DragEventArgs e)
    {
        return e.Data.GetDataPresent(DataFormats.FileDrop)
            && e.Data.GetData(DataFormats.FileDrop) is string[] files
            && files.Any(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
    }
}
