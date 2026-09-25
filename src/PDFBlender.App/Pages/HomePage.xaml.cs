using Microsoft.Win32;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PDFBlender.Pdf;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PDFBlender.App.Pages;

public partial class HomePage : Page
{
    private readonly AppSession _session;
    private readonly UiNotificationService _notifications;
    private readonly IBlendProcessor _processor = new PdfBlendProcessor();
    private bool _initializing;
    private CancellationTokenSource? _processingCts;

    internal HomePage(AppSession session, UiNotificationService notifications)
    {
        _session = session;
        _notifications = notifications;
        InitializeComponent();
        InitializeSelectors();
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        if (!IsInitialized)
        {
            return;
        }

        UiText.SetLanguage(_session.Preferences.Language);
        PageTitle.Text = UiText.Get("appName");
        PageSubtitle.Text = UiText.Get("subtitle");
        DropTitleText.Text = UiText.Get("dropTitle");
        DropHintText.Text = UiText.Get("dropHint");
        LocalProcessingText.Text = UiText.Get("localProcessing");
        BrowseButtonText.Text = UiText.Get("browse");
        ProfileLabel.Text = UiText.Get("profile");
        OutputLabel.Text = UiText.Get("output");
        BlendButtonText.Text = UiText.Get("blend");
        CancelButtonText.Text = UiText.Get("cancel");
        BlendLevelRailControl.SetCaption(UiText.Get("blendLevel"));
        BlendLevelRailControl.SetAccessibilityName(UiText.Get("blendLevel"));
        BlendLevelRailControl.SetLevels(BuildLevelItems());
        BlendLevelRailControl.SelectedIndex = Math.Clamp(_session.Preferences.SelectedBlendLevel, 0, _session.Presets.Count - 1);
        UpdateProfileLabels();
        SetInputPath(_session.InputPath, notify: false);
    }

    private void InitializeSelectors()
    {
        _initializing = true;
        BlendLevelRailControl.SetLevels(BuildLevelItems());
        ProfileCombo.ItemsSource = _session.Profiles.Select(profile => ProfileDisplayName(profile.Id)).ToList();
        ProfileCombo.SelectedIndex = _session.SelectedProfileIndex;
        _initializing = false;
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
        if (dialog.ShowDialog() == true)
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
            if (pdf is not null) SetInputPath(pdf);
        }
        e.Handled = true;
    }

    private async void BlendButton_Click(object sender, RoutedEventArgs e)
    {
        if (_session.InputPath is null)
        {
            StatusText.Text = UiText.Get("ready");
            return;
        }
        if (_processingCts is not null) return;

        var outputPath = OutputText.Text;
        if (string.IsNullOrWhiteSpace(outputPath) || outputPath == "—")
        {
            StatusText.Text = UiText.Get("outputInvalid");
            return;
        }

        _processingCts = new CancellationTokenSource();
        SetProcessingState(true);
        var progressNotification = _notifications.Show(UiNotificationKind.Progress, UiText.Get("blend"), UiText.Get("processing"), autoDismiss: false);
        var progress = new Progress<BlendProgress>(value =>
        {
            ProcessingProgress.Value = value.Fraction * 100;
            StatusText.Text = $"{value.Stage} {value.CompletedPages}/{value.TotalPages}";
            _notifications.UpdateProgress(progressNotification, StatusText.Text, value.Fraction);
        });
        try
        {
            var result = await _processor.ProcessAsync(new BlendRequest(_session.InputPath, outputPath, _session.Settings), progress, _processingCts.Token);
            StatusText.Text = $"{UiText.Get("completed")}: {Path.GetFileName(result.OutputPath)}";
            _notifications.Dismiss(progressNotification);
            _notifications.Show(UiNotificationKind.Success, UiText.Get("completed"), Path.GetFileName(result.OutputPath));
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = UiText.Get("cancelled");
            _notifications.Dismiss(progressNotification);
            _notifications.Show(UiNotificationKind.Info, UiText.Get("cancelled"), UiText.Get("ready"));
        }
        catch (Exception exception)
        {
            StatusText.Text = $"{UiText.Get("processingError")}: {exception.Message}";
            _notifications.Dismiss(progressNotification);
            _notifications.Show(UiNotificationKind.Error, UiText.Get("processingError"), exception.Message, autoDismiss: false);
        }
        finally
        {
            _processingCts.Dispose();
            _processingCts = null;
            SetProcessingState(false);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => _processingCts?.Cancel();

    private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || ProfileCombo.SelectedIndex < 0) return;
        _session.SelectProfile(ProfileCombo.SelectedIndex);
        StatusText.Text = $"{UiText.Get("profile")}: {ProfileDisplayName(_session.Profiles[ProfileCombo.SelectedIndex].Id)}";
    }

    private void BlendLevelRailControl_SelectionChanged(object? sender, BlendLevelChangedEventArgs e)
    {
        if (_initializing || e.SelectedIndex < 0 || e.SelectedIndex >= _session.Presets.Count) return;
        _session.SetBlendLevel(e.SelectedIndex);
        StatusText.Text = e.Item.Summary;
    }

    private void SetInputPath(string? path, bool notify = true)
    {
        if (notify) _session.SetInputPath(path);
        var hasFile = path is not null;
        ReadyText.Text = hasFile ? UiText.Get("selected") : UiText.Get("ready");
        FileNameText.Text = hasFile ? Path.GetFileName(path) : string.Empty;
        BlendButton.IsEnabled = hasFile;
        OutputText.Text = hasFile
            ? Path.Combine(Path.GetDirectoryName(path!) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(path)}_blended.pdf")
            : "—";
        StatusText.Text = hasFile ? Path.GetFileName(path) : UiText.Get("ready");
    }

    private void SetProcessingState(bool processing)
    {
        BlendButton.IsEnabled = !processing && _session.InputPath is not null;
        BrowseButton.IsEnabled = !processing;
        ProfileCombo.IsEnabled = !processing;
        BlendLevelRailControl.IsEnabled = !processing;
        ProcessingProgress.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
        if (!processing) ProcessingProgress.Value = 0;
    }

    private void UpdateProfileLabels()
    {
        var selected = ProfileCombo.SelectedIndex;
        _initializing = true;
        ProfileCombo.ItemsSource = _session.Profiles.Select(profile => ProfileDisplayName(profile.Id)).ToList();
        ProfileCombo.SelectedIndex = Math.Clamp(selected < 0 ? _session.SelectedProfileIndex : selected, 0, Math.Max(0, _session.Profiles.Count - 1));
        _initializing = false;
    }

    private IReadOnlyList<BlendLevelItem> BuildLevelItems() =>
    [
        new(_session.Presets[0].DisplayName, UiText.Get("stirSummary")),
        new(_session.Presets[1].DisplayName, UiText.Get("blendSummary")),
        new(_session.Presets[2].DisplayName, UiText.Get("pulseSummary"))
    ];

    private static string ProfileDisplayName(string id) => id switch
    {
        "research" => UiText.Get("profileResearch"),
        "public" => UiText.Get("profilePublic"),
        _ => UiText.Get("profileDefault")
    };

    private static bool HasPdfDrop(DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop) &&
        e.Data.GetData(DataFormats.FileDrop) is string[] files &&
        files.Any(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
}
