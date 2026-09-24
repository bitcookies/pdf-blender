using Microsoft.Win32;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PDFBlender.Pdf;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PDFBlender.App;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<BlendPreset> _presets = Presets.All;
    private readonly List<ProfileDefinition> _profiles = ProfileStore.Load().ToList();
    private BlendSettings _settings = BlendSettings.Default;
    private string? _inputPath;
    private UiLanguage _language = UiLanguage.System;
    private bool _darkTheme = true;
    private bool _initializing;
    private CancellationTokenSource? _processingCts;
    private readonly IBlendProcessor _processor = new PdfBlendProcessor();

    public MainWindow()
    {
        InitializeComponent();
        InitializeSelectors();
        ApplyLanguage();
        ApplyTheme();
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

        foreach (var preset in _presets)
        {
            ModeCombo.Items.Add(new ComboBoxItem { Content = preset.DisplayName, Tag = preset });
        }

        ModeCombo.SelectedIndex = 1;
        LanguageCombo.Items.Add("System");
        LanguageCombo.Items.Add("English");
        LanguageCombo.Items.Add("简体中文");
        LanguageCombo.SelectedIndex = 0;
        _initializing = false;
        UpdateModeSummary();
    }

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
            var pdf = files.FirstOrDefault(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
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
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _settings = dialog.Settings;
            if (ProfileCombo.SelectedIndex >= 0 && ProfileCombo.SelectedIndex < _profiles.Count)
            {
                _profiles[ProfileCombo.SelectedIndex] = _profiles[ProfileCombo.SelectedIndex] with { Settings = _settings };
                try { ProfileStore.Save(_profiles); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            ModeSummaryText.Text = UiText.Get("customSettings");
            StatusText.Text = UiText.Get("settingsSaved");
        }
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _darkTheme = !_darkTheme;
        ApplyTheme();
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || LanguageCombo.SelectedIndex < 0)
        {
            return;
        }

        _language = LanguageCombo.SelectedIndex switch
        {
            1 => UiLanguage.English,
            2 => UiLanguage.SimplifiedChinese,
            _ => UiLanguage.System
        };
        ApplyLanguage();
    }

    private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing && ProfileCombo.SelectedIndex >= 0 && ProfileCombo.SelectedIndex < _profiles.Count)
        {
            _settings = _profiles[ProfileCombo.SelectedIndex].Settings;
            UpdateModeSummary();
            StatusText.Text = $"{UiText.Get("profile")}: {ProfileDisplayName(_profiles[ProfileCombo.SelectedIndex].Id)}";
        }
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing && ModeCombo.SelectedItem is ComboBoxItem { Tag: BlendPreset preset })
        {
            _settings = preset.Settings;
            UpdateModeSummary();
        }
    }

    private void ApplyLanguage()
    {
        UiText.SetLanguage(_language);
        SubtitleText.Text = UiText.Get("subtitle");
        DropTitleText.Text = UiText.Get("dropTitle");
        DropHintText.Text = UiText.Get("dropHint");
        LocalProcessingText.Text = UiText.Get("localProcessing");
        SetupTitleText.Text = UiText.Get("setup");
        BrowseButton.Content = UiText.Get("browse");
        ProfileLabel.Text = UiText.Get("profile");
        ModeLabel.Text = UiText.Get("mode");
        OutputLabel.Text = UiText.Get("output");
        OutputHintText.Text = UiText.Get("outputHint");
        BlendButton.Content = UiText.Get("blend");
        CancelButton.Content = UiText.Get("cancel");
        SettingsButton.Content = UiText.Get("settings");
        ThemeButton.ToolTip = UiText.Get("themeTooltip");
        LanguageLabel.Text = UiText.Get("language");
        ThemeButton.Content = _darkTheme ? "☼" : "☾";
        ReadyText.Text = _inputPath is null ? UiText.Get("ready") : UiText.Get("selected");
        StatusText.Text = _inputPath is null ? UiText.Get("ready") : Path.GetFileName(_inputPath);
        UpdateProfileLabels();
        UpdateModeSummary();
    }

    private void UpdateProfileLabels()
    {
        var selectedIndex = Math.Max(ProfileCombo.SelectedIndex, 0);
        ProfileCombo.Items.Clear();
        foreach (var profile in _profiles)
        {
            ProfileCombo.Items.Add(ProfileDisplayName(profile.Id));
        }
        ProfileCombo.SelectedIndex = Math.Min(selectedIndex, ProfileCombo.Items.Count - 1);
    }

    private static string ProfileDisplayName(string id) => id switch
    {
        "research" => UiText.Get("profileResearch"),
        "public" => UiText.Get("profilePublic"),
        _ => UiText.Get("profileDefault")
    };

    private void ApplyTheme()
    {
        var resources = Application.Current.Resources;
        SetBrush(resources, "WindowBackgroundBrush", _darkTheme ? "#17181D" : "#F4F1EF");
        SetBrush(resources, "SurfaceBrush", _darkTheme ? "#23252D" : "#FFFFFF");
        SetBrush(resources, "SurfaceRaisedBrush", _darkTheme ? "#2C2F38" : "#F0ECEA");
        SetBrush(resources, "SurfaceMutedBrush", _darkTheme ? "#1E2027" : "#F8F6F4");
        SetBrush(resources, "BorderBrush", _darkTheme ? "#3B3E48" : "#DED8D3");
        SetBrush(resources, "TextBrush", _darkTheme ? "#F6F4F2" : "#272329");
        SetBrush(resources, "MutedTextBrush", _darkTheme ? "#A9A8B0" : "#716A70");
        SetBrush(resources, "AccentBrush", _darkTheme ? "#FF6C62" : "#D94D54");
        SetBrush(resources, "AccentDarkBrush", _darkTheme ? "#D84C52" : "#BC3F47");
        SetBrush(resources, "AccentTextBrush", _darkTheme ? "#20171A" : "#FFFFFF");
        Background = (Brush)resources["WindowBackgroundBrush"];
        ThemeButton.Content = _darkTheme ? "☼" : "☾";
    }

    private static void SetBrush(ResourceDictionary resources, string key, string hex)
    {
        resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    }

    private void SetInputPath(string? path)
    {
        _inputPath = path;
        var hasFile = path is not null;
        ReadyText.Text = hasFile ? UiText.Get("selected") : UiText.Get("ready");
        FileNameText.Text = hasFile ? Path.GetFileName(path) : string.Empty;
        BlendButton.IsEnabled = hasFile;
        OutputText.Text = hasFile ? Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}_blended.pdf") : "—";
        StatusText.Text = hasFile ? Path.GetFileName(path) : UiText.Get("ready");
    }

    private void SetProcessingState(bool processing)
    {
        BlendButton.IsEnabled = !processing && _inputPath is not null;
        BrowseButton.IsEnabled = !processing;
        ProfileCombo.IsEnabled = !processing;
        ModeCombo.IsEnabled = !processing;
        LanguageCombo.IsEnabled = !processing;
        ProcessingProgress.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
        if (!processing)
        {
            ProcessingProgress.Value = 0;
        }
    }

    private void UpdateModeSummary()
    {
        if (ModeCombo.SelectedItem is ComboBoxItem { Tag: BlendPreset preset })
        {
            ModeSummaryText.Text = preset.Settings == _settings
                ? preset.Summary
                : UiText.Get("customSettings");
        }
    }

    private static bool HasPdfDrop(DragEventArgs e)
    {
        return e.Data.GetDataPresent(DataFormats.FileDrop)
            && e.Data.GetData(DataFormats.FileDrop) is string[] files
            && files.Any(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
    }
}
