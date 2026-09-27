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
    private readonly Action _openConfiguration;
    private readonly IBlendProcessor _processor = new PdfBlendProcessor();
    private bool _initializing;
    private CancellationTokenSource? _processingCts;

    internal HomePage(AppSession session, UiNotificationService notifications, Action openConfiguration)
    {
        _session = session;
        _notifications = notifications;
        _openConfiguration = openConfiguration;
        InitializeComponent();
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        if (!IsInitialized) return;
        _initializing = true;
        UiText.SetLanguage(_session.Preferences.Language);
        PageTitle.Text = UiText.Get("appName");
        PageSubtitle.Text = UiText.Get("subtitle");
        DropTitleText.Text = UiText.Get("dropTitle");
        DropHintText.Text = UiText.Get("dropHint");
        BrowseButtonText.Text = UiText.Get("browse");
        ConfigurationButtonText.Text = UiText.Get("configuration");
        BlendButtonText.Text = UiText.Get("blend");
        CancelButtonText.Text = UiText.Get("cancel");
        BlendLevelRailControl.SetCaption(UiText.Get("blendLevel"));
        BlendLevelRailControl.SetAccessibilityName(UiText.Get("blendLevel"));
        BlendLevelRailControl.SetLevels(BuildLevelItems());
        BlendLevelRailControl.SelectedIndex = _session.SelectedBlendLevel;
        SetInputPath(_session.InputPath, notify: false);
        if (_session.SlotConfigurations.All(configuration => configuration is null))
        {
            StatusText.Text = UiText.Get("noBlendSlots");
        }
        _initializing = false;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf", CheckFileExists = true, Multiselect = false, Title = UiText.Get("browse") };
        if (dialog.ShowDialog() == true) SetInputPath(dialog.FileName);
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
        if (_session.InputPath is null) { StatusText.Text = UiText.Get("ready"); return; }
        if (_session.SlotConfigurations.All(configuration => configuration is null)) { StatusText.Text = UiText.Get("noBlendSlots"); return; }
        if (_processingCts is not null) return;

        var inputPath = _session.InputPath;
        var outputPath = Path.Combine(Path.GetDirectoryName(inputPath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(inputPath)}_blended.pdf");
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
            var result = await _processor.ProcessAsync(new BlendRequest(inputPath, outputPath, _session.Settings), progress, _processingCts.Token);
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

    private void ConfigurationButton_Click(object sender, RoutedEventArgs e) => _openConfiguration();
    private void CancelButton_Click(object sender, RoutedEventArgs e) => _processingCts?.Cancel();

    private void BlendLevelRailControl_SelectionChanged(object? sender, BlendLevelChangedEventArgs e)
    {
        if (_initializing || e.SelectedIndex < 0 || e.SelectedIndex >= _session.SlotConfigurations.Count || _session.SlotConfigurations[e.SelectedIndex] is null) return;
        _session.SetBlendLevel(e.SelectedIndex);
        StatusText.Text = e.Item.Summary;
    }

    private void SetInputPath(string? path, bool notify = true)
    {
        if (notify) _session.SetInputPath(path);
        var hasFile = path is not null;
        ReadyText.Text = hasFile ? UiText.Get("selected") : UiText.Get("ready");
        FileNameText.Text = hasFile ? Path.GetFileName(path) : string.Empty;
        BlendButton.IsEnabled = hasFile && _session.SlotConfigurations.Any(configuration => configuration is not null);
        StatusText.Text = _session.SlotConfigurations.All(configuration => configuration is null)
            ? UiText.Get("noBlendSlots")
            : hasFile ? Path.GetFileName(path) : UiText.Get("ready");
    }

    private void SetProcessingState(bool processing)
    {
        BlendButton.IsEnabled = !processing && _session.InputPath is not null && _session.SlotConfigurations.Any(configuration => configuration is not null);
        BrowseButton.IsEnabled = !processing;
        ConfigurationButton.IsEnabled = !processing;
        BlendLevelRailControl.IsEnabled = !processing;
        ProcessingProgress.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
        if (!processing) ProcessingProgress.Value = 0;
    }

    private IReadOnlyList<BlendLevelItem> BuildLevelItems() =>
        _session.SlotConfigurations
            .Select((configuration, index) => configuration is null
                ? new BlendLevelItem($"{index + 1}", string.Empty, false)
                : new BlendLevelItem(configuration.Name, $"{UiText.Get("intensity")} {configuration.Intensity}"))
            .ToList();

    private static bool HasPdfDrop(DragEventArgs e) => e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
}
