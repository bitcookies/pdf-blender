using Microsoft.Win32;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PDFBlender.Pdf;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
    private readonly List<PreviewVisual> _previewVisuals = [];

    private sealed class PreviewVisual
    {
        public required string Path { get; init; }
        public required FrameworkElement Container { get; init; }
        public required Image Preview { get; init; }
        public required Image Fallback { get; init; }
        public required Border LoadingOverlay { get; init; }
        public required Wpf.Ui.Controls.Button RemoveButton { get; init; }
    }

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
        AdditionalFilesHint.Text = UiText.Get("addMoreFiles");
        SelectedFileLabel.Text = UiText.Get("selected");
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
        if (dialog.ShowDialog() == true) AppendInputPaths(dialog.FileNames);
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
            AppendInputPaths(pdfs);
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
        PreviewStrip.Children.Clear();
        _previewVisuals.Clear();
        foreach (var selectedPath in selectedPaths)
        {
            var preview = CreatePreviewVisual(selectedPath);
            _previewVisuals.Add(preview);
            PreviewStrip.Children.Add(preview.Container);
        }
        BlendButton.IsEnabled = hasFile && _session.SlotConfigurations.Any(configuration => configuration is not null);
        StatusText.Text = _session.SlotConfigurations.All(configuration => configuration is null) ? UiText.Get("noBlendSlots") : hasFile ? Path.GetFileName(path) : UiText.Get("ready");
        if (hasFile) _ = LoadPreviewsAsync(selectedPaths, generation);
    }

    private void AppendInputPaths(IEnumerable<string>? paths)
    {
        SetInputPaths(_session.InputPaths.Concat(paths ?? []));
    }

    private PreviewVisual CreatePreviewVisual(string path)
    {
        var preview = new Image { Width = 68, Height = 104, Stretch = System.Windows.Media.Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var fallback = new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/PDFBlender.App;component/Assets/pdf-icon.png")),
            Width = 46,
            Height = 46,
            Stretch = System.Windows.Media.Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var loadingOverlay = new Border
        {
            Background = (Brush)FindResource("SurfaceRaisedBrush"),
            Opacity = 0.92,
            Child = new Wpf.Ui.Controls.ProgressRing()
        };
        if (loadingOverlay.Child is Wpf.Ui.Controls.ProgressRing ring)
        {
            ring.IsIndeterminate = true;
            ring.Width = 24;
            ring.Height = 24;
            ring.HorizontalAlignment = HorizontalAlignment.Center;
            ring.VerticalAlignment = VerticalAlignment.Center;
        }
        var grid = new Grid();
        grid.ClipToBounds = false;
        grid.Children.Add(preview);
        grid.Children.Add(fallback);
        grid.Children.Add(loadingOverlay);
        var removeButton = new Wpf.Ui.Controls.Button
        {
            Width = 18,
            Height = 18,
            Padding = new Thickness(0),
            Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FocusVisualStyle = null,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = UiText.Get("removePdf"),
            Content = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Dismiss16, FontSize = 11 }
        };
        AutomationProperties.SetName(removeButton, UiText.Get("removePdf"));
        removeButton.Click += (_, _) => RemoveInputPath(path);
        var removeSurface = new Border
        {
            Width = 21,
            Height = 21,
            CornerRadius = new CornerRadius(11),
            Background = (Brush)FindResource("CardBrush"),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 3, 0),
            Child = removeButton
        };
        Panel.SetZIndex(removeSurface, 10);
        var card = new Border
        {
            Width = 88,
            Height = 141,
            Padding = new Thickness(6),
            Background = (Brush)FindResource("SurfaceRaisedBrush"),
            BorderBrush = (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = false,
            Child = grid,
            ToolTip = path
        };
        var container = new Grid
        {
            Width = 99,
            Height = 145,
            Margin = new Thickness(0, 0, 8, 0),
            ClipToBounds = false
        };
        card.HorizontalAlignment = HorizontalAlignment.Left;
        card.VerticalAlignment = VerticalAlignment.Bottom;
        container.Children.Add(card);
        container.Children.Add(removeSurface);
        return new PreviewVisual { Path = path, Container = container, Preview = preview, Fallback = fallback, LoadingOverlay = loadingOverlay, RemoveButton = removeButton };
    }

    private void RemoveInputPath(string path)
    {
        if (_processingCts is not null) return;
        SetInputPaths(_session.InputPaths.Where(item => !string.Equals(item, path, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task LoadPreviewsAsync(IReadOnlyList<string> paths, int generation)
    {
        var cts = new CancellationTokenSource();
        _previewCts = cts;
        try
        {
            foreach (var path in paths)
            {
                cts.Token.ThrowIfCancellationRequested();
                var visual = _previewVisuals.FirstOrDefault(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase));
                if (visual is null) continue;
                try
                {
                    var bitmap = await PdfPreviewService.RenderFirstPageAsync(path, cts.Token);
                    if (generation != _previewGeneration || cts.IsCancellationRequested) return;
                    visual.Preview.Source = bitmap;
                    visual.Fallback.Visibility = Visibility.Collapsed;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception)
                {
                    visual.Fallback.Visibility = Visibility.Visible;
                }
                finally
                {
                    visual.LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (generation == _previewGeneration)
            {
                _previewCts = null;
            }
            cts.Dispose();
        }
    }

    private void SetProcessingState(bool processing)
    {
        BlendButton.IsEnabled = processing || (_session.InputPaths.Count > 0 && _session.SlotConfigurations.Any(configuration => configuration is not null));
        BrowseButton.IsEnabled = !processing;
        foreach (var visual in _previewVisuals)
            visual.RemoveButton.IsEnabled = !processing;
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
