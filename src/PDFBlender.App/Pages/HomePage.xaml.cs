using Microsoft.Win32;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PDFBlender.Pdf;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace PDFBlender.App.Pages;

public partial class HomePage : Page
{
    private readonly AppSession _session;
    private readonly UiNotificationService _notifications;
    private readonly Action _openConfiguration;
    private readonly IBlendProcessor _processor = new PdfBlendProcessor();
    private bool _initializing;
    private CancellationTokenSource? _processingCts;
    private CancellationTokenSource? _previewCts;
    private int _previewGeneration;

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
        PageSlogan.Text = UiText.Get("homeSlogan");
        DropTitleText.Text = UiText.Get("dropTitle");
        DropHintText.Text = UiText.Get("dropHint");
        BrowseButtonText.Text = UiText.Get("browse");
        AutomationProperties.SetName(BrowseButton, UiText.Get("browse"));
        ReplacePdfButtonText.Text = UiText.Get("replacePdf");
        AutomationProperties.SetName(ReplacePdfButton, UiText.Get("replacePdf"));
        SelectedFileLabel.Text = UiText.Get("selected");
        PreviewLoadingText.Text = UiText.Get("previewLoading");
        RemovePdfButton.ToolTip = UiText.Get("removePdf");
        AutomationProperties.SetName(RemovePdfButton, UiText.Get("removePdf"));
        BlendButtonText.Text = UiText.Get("blend");
        AutomationProperties.SetName(BlendButton, UiText.Get("blend"));
        BlendLevelRailControl.SetCaption(UiText.Get("blendLevel"));
        BlendLevelRailControl.SetAccessibilityName(UiText.Get("blendLevel"));
        BlendLevelRailControl.SetEditAccessibilityName(UiText.Get("editBlendLevel"));
        BlendLevelRailControl.SetLevels(BuildLevelItems());
        BlendLevelRailControl.SelectedIndex = _session.SelectedBlendLevel;
        SetInputPaths(_session.InputPaths, notify: false);
        _initializing = false;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf", CheckFileExists = true, Multiselect = true, Title = UiText.Get("browse") };
        if (dialog.ShowDialog() == true) SetInputPaths(dialog.FileNames);
    }

    private void DropCard_DragEnter(object sender, DragEventArgs e)
    {
        DropTarget_DragOver(sender, e);
        if (e.Effects != DragDropEffects.None)
        {
            DropCard.BorderBrush = (Brush)FindResource("AccentBrush");
            DropCard.Background = (Brush)FindResource("AccentSoftBrush");
            DropDash.Stroke = (Brush)FindResource("AccentBrush");
        }
    }

    private void DropCard_DragLeave(object sender, DragEventArgs e)
    {
        DropCard.BorderBrush = (Brush)FindResource("BorderBrush");
        DropCard.Background = (Brush)FindResource("CardBrush");
        DropDash.Stroke = (Brush)FindResource("BorderBrush");
    }

    private void DropTarget_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasPdfDrop(e) && _processingCts is null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropTarget_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            var pdfs = files.Where(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
            SetInputPaths(pdfs);
        }
        DropCard_DragLeave(sender, e);
        e.Handled = true;
    }

    private async void BlendButton_Click(object sender, RoutedEventArgs e)
    {
        if (_processingCts is not null)
        {
            _processingCts.Cancel();
            return;
        }
        if (_session.InputPaths.Count == 0) { StatusText.Text = UiText.Get("ready"); return; }
        if (_session.SlotConfigurations.All(configuration => configuration is null)) { StatusText.Text = UiText.Get("noBlendSlots"); return; }
        if (_processingCts is not null) return;
        var inputPaths = _session.InputPaths.ToArray();
        _processingCts = new CancellationTokenSource();
        SetProcessingState(true);
        var progressNotification = _notifications.Show(UiNotificationKind.Progress, UiText.Get("blend"), UiText.Get("processing"), autoDismiss: false);
        var completedFiles = 0;
        var progress = new Progress<BlendProgress>(value =>
        {
            var overall = (completedFiles + value.Fraction) / inputPaths.Length;
            ProcessingProgress.Value = overall * 100;
            StatusText.Text = $"{value.Stage} {completedFiles + 1}/{inputPaths.Length} · {value.CompletedPages}/{value.TotalPages}";
            _notifications.UpdateProgress(progressNotification, StatusText.Text, overall);
        });
        try
        {
            var outputNames = new List<string>();
            foreach (var inputPath in inputPaths)
            {
                var outputPath = Path.Combine(Path.GetDirectoryName(inputPath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(inputPath)}_blended.pdf");
                var result = await _processor.ProcessAsync(new BlendRequest(inputPath, outputPath, _session.Settings), progress, _processingCts.Token);
                outputNames.Add(Path.GetFileName(result.OutputPath));
                completedFiles++;
            }
            StatusText.Text = $"{UiText.Get("completed")}: {string.Format(UiText.Get("completedCount"), outputNames.Count)}";
            _notifications.Dismiss(progressNotification);
            _notifications.Show(UiNotificationKind.Success, UiText.Get("completed"), string.Join(", ", outputNames));
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

    private void RemovePdfButton_Click(object sender, RoutedEventArgs e) => SetInputPaths([]);
    private void BlendLevelRailControl_EditRequested(object? sender, RoutedEventArgs e) => _openConfiguration();
    private void BlendLevelRailControl_SelectionChanged(object? sender, BlendLevelChangedEventArgs e)
    {
        if (_initializing || e.SelectedIndex < 0 || e.SelectedIndex >= _session.SlotConfigurations.Count || _session.SlotConfigurations[e.SelectedIndex] is null) return;
        _session.SetBlendLevel(e.SelectedIndex);
        StatusText.Text = e.Item.Summary;
    }

    private void SetInputPaths(IEnumerable<string>? paths, bool notify = true)
    {
        var selectedPaths = (paths ?? []).Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (notify) _session.SetInputPaths(selectedPaths);
        _previewCts?.Cancel();
        _previewCts?.Dispose();
        _previewCts = null;
        var generation = ++_previewGeneration;
        var path = selectedPaths.FirstOrDefault();
        var hasFile = selectedPaths.Length > 0;
        EmptyDropContent.Visibility = hasFile ? Visibility.Collapsed : Visibility.Visible;
        SelectedDropContent.Visibility = hasFile ? Visibility.Visible : Visibility.Collapsed;
        FileNameText.Text = hasFile ? Path.GetFileName(path) : string.Empty;
        SelectedCountText.Text = selectedPaths.Length > 1 ? string.Format(UiText.Get("selectedCount"), selectedPaths.Length) : string.Empty;
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Visible;
        PreviewFallbackImage.Visibility = hasFile ? Visibility.Visible : Visibility.Collapsed;
        PreviewErrorText.Visibility = Visibility.Collapsed;
        PreviewLoadingOverlay.Visibility = hasFile ? Visibility.Visible : Visibility.Collapsed;
        BlendButton.IsEnabled = hasFile && _session.SlotConfigurations.Any(configuration => configuration is not null);
        StatusText.Text = _session.SlotConfigurations.All(configuration => configuration is null) ? UiText.Get("noBlendSlots") : hasFile ? Path.GetFileName(path) : UiText.Get("ready");
        if (hasFile && path is not null) _ = LoadPreviewAsync(path, generation);
    }

    private async Task LoadPreviewAsync(string path, int generation)
    {
        var cts = new CancellationTokenSource();
        _previewCts = cts;
        try
        {
            var bitmap = await PdfPreviewService.RenderFirstPageAsync(path, cts.Token);
            if (generation != _previewGeneration || cts.IsCancellationRequested) return;
            PreviewImage.Source = bitmap;
            PreviewFallbackImage.Visibility = Visibility.Collapsed;
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (generation != _previewGeneration) return;
            PreviewErrorText.Text = UiText.Get("previewFailed");
            PreviewErrorText.Visibility = Visibility.Visible;
            PreviewFallbackImage.Visibility = Visibility.Visible;
        }
        finally
        {
            if (generation == _previewGeneration)
            {
                PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
                _previewCts = null;
            }
            cts.Dispose();
        }
    }

    private void SetProcessingState(bool processing)
    {
        BlendButton.IsEnabled = processing || (_session.InputPaths.Count > 0 && _session.SlotConfigurations.Any(configuration => configuration is not null));
        BrowseButton.IsEnabled = !processing;
        ReplacePdfButton.IsEnabled = !processing;
        RemovePdfButton.IsEnabled = !processing;
        BlendLevelRailControl.IsEnabled = !processing;
        ProcessingProgress.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
        BlendButtonText.Text = UiText.Get(processing ? "cancel" : "blend");
        BlendButtonIcon.Symbol = processing ? Wpf.Ui.Controls.SymbolRegular.Dismiss24 : Wpf.Ui.Controls.SymbolRegular.Play24;
        AutomationProperties.SetName(BlendButton, UiText.Get(processing ? "cancel" : "blend"));
        if (!processing) ProcessingProgress.Value = 0;
    }

    private IReadOnlyList<BlendLevelItem> BuildLevelItems() => _session.SlotConfigurations.Select((configuration, index) => configuration is null ? new BlendLevelItem($"{index + 1}", string.Empty, false) : new BlendLevelItem(configuration.Name, $"{UiText.Get("intensity")} {configuration.Intensity}")).ToList();

    private static bool HasPdfDrop(DragEventArgs e) => e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase));
}
