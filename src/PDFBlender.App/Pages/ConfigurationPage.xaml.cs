using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PDFBlender.Core.Models;
using UiCard = Wpf.Ui.Controls.CardControl;
using UiButton = Wpf.Ui.Controls.Button;
using ControlAppearance = Wpf.Ui.Controls.ControlAppearance;

namespace PDFBlender.App.Pages;

public partial class ConfigurationPage : Page
{
    private const string ConfigurationDragFormat = "PDFBlender.ConfigurationId";
    private readonly AppSession _session;
    private readonly DialogService _dialogs;
    private Point _dragStart;
    private UiCard? _dragCandidate;
    private bool _dragInProgress;

    internal ConfigurationPage(AppSession session, DialogService dialogs)
    {
        _session = session;
        _dialogs = dialogs;
        InitializeComponent();
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        UiText.SetLanguage(_session.Preferences.Language);
        TitleText.Text = UiText.Get("configuration");
        SubtitleText.Text = UiText.Get("configurationSubtitle");
        SlotsHeader.Text = UiText.Get("blendSlots");
        SlotsHint.Text = UiText.Get("blendSlotsHint");
        ConfigurationsHeader.Text = UiText.Get("blendConfigurations");
        AddButtonText.Text = UiText.Get("addConfiguration");
        EmptyText.Text = _session.AllConfigurations.Count == 0 ? UiText.Get("noConfigurations") : string.Empty;
        ErrorText.Text = string.Empty;
        BuildSlots();
        BuildCards(_session.AllConfigurations);
    }

    private void BuildSlots()
    {
        SlotsPanel.Children.Clear();
        SlotsPanel.ColumnDefinitions.Clear();
        for (var index = 0; index < 5; index++) SlotsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var index = 0; index < 5; index++)
        {
            var slot = CreateSlot(_session.SlotConfigurations[index], index);
            Grid.SetColumn(slot, index);
            SlotsPanel.Children.Add(slot);
        }
    }

    private UiCard CreateSlot(BlendConfiguration? configuration, int index)
    {
        var card = new UiCard
        {
            Margin = new Thickness(index == 0 ? 0 : 5, 0, index == 4 ? 0 : 5, 0), Padding = new Thickness(9, 8, 9, 8), MinHeight = 72,
            Background = (Brush)FindResource(configuration is null ? "SurfaceMutedBrush" : "CardBrush"), BorderBrush = (Brush)FindResource(configuration is null ? "BorderBrush" : "AccentBrush"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), AllowDrop = true, Tag = index
        };
        var root = new StackPanel();
        var header = new DockPanel();
        if (configuration is not null)
        {
            var remove = MakeIconAction("Dismiss16", UiText.Get("remove"), true);
            remove.Padding = new Thickness(2);
            remove.Margin = new Thickness(0);
            remove.Click += (_, _) => _session.RemoveFromSlots(configuration.Id);
            DockPanel.SetDock(remove, Dock.Right);
            header.Children.Add(remove);
        }
        header.Children.Add(new TextBlock { Text = $"{index + 1}", Foreground = (Brush)FindResource("MutedTextBrush"), FontSize = 10, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        root.Children.Add(header);
        root.Children.Add(new TextBlock { Text = configuration?.Name ?? UiText.Get("emptySlot"), FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource(configuration is null ? "MutedTextBrush" : "TextBrush"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 4, 0, 0) });
        if (configuration is not null) root.Children.Add(new TextBlock { Text = $"{UiText.Get("intensity")} {configuration.Intensity}", FontSize = 10, Foreground = IntensityBrush(configuration.Intensity), Margin = new Thickness(0, 3, 0, 0) });
        card.Content = root;
        card.AddHandler(UIElement.PreviewDragOverEvent, new DragEventHandler(Slot_DragOver), true);
        card.AddHandler(UIElement.DragLeaveEvent, new DragEventHandler((_, _) => ResetSlotVisual(card)), true);
        card.AddHandler(UIElement.PreviewDropEvent, new DragEventHandler(Slot_Drop), true);
        if (configuration is not null)
        {
            AttachDragHandlers(card, configuration.Id);
        }
        return card;
    }

    private void BuildCards(IEnumerable<BlendConfiguration> configurations)
    {
        ConfigurationCardsPanel.Children.Clear();
        foreach (var configuration in configurations)
            ConfigurationCardsPanel.Children.Add(CreateCard(configuration));
    }

    private UiCard CreateCard(BlendConfiguration configuration)
    {
        var card = new UiCard { Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(12, 10, 10, 10), MinHeight = 126, Background = (Brush)FindResource("CardBrush"), BorderBrush = IntensityBrush(configuration.Intensity), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ToolTip = UiText.Get("dragToSlots"), AllowDrop = false, Focusable = false };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var details = new StackPanel();
        details.Children.Add(new TextBlock { Text = configuration.Name, FontSize = 14, FontWeight = FontWeights.SemiBold });
        details.Children.Add(new TextBlock { Text = FeatureSummary(configuration), Foreground = (Brush)FindResource("MutedTextBrush"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 12, 0) });
        details.Children.Add(CreateIntensityBar(configuration.Intensity));
        Grid.SetColumn(details, 0); grid.Children.Add(details);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var edit = MakeIconAction("Edit24", UiText.Get("edit"), !configuration.IsLocked); edit.Click += (_, _) => OpenEditor(configuration, false);
        var lockButton = MakeIconAction(configuration.IsLocked ? "LockClosed24" : "LockOpen24", configuration.IsLocked ? UiText.Get("unlock") : UiText.Get("lock"), true); lockButton.Click += (_, _) => _session.SetConfigurationLocked(configuration.Id, !configuration.IsLocked);
        var delete = MakeIconAction("Delete24", UiText.Get("delete"), !configuration.IsLocked);
        delete.Click += async (_, _) =>
        {
            if (configuration.IsLocked) return;
            var confirmed = await _dialogs.ShowConfirmationAsync(
                UiText.Get("delete"),
                UiText.Get("deleteConfigurationConfirm"),
                UiText.Get("delete"),
                UiText.Get("cancel"),
                Wpf.Ui.Controls.SymbolRegular.Delete24);
            if (confirmed) _session.DeleteConfiguration(configuration.Id);
        };
        actions.Children.Add(edit); actions.Children.Add(lockButton); actions.Children.Add(delete); Grid.SetColumn(actions, 1); grid.Children.Add(actions); card.Content = grid;
        AttachDragHandlers(card, configuration.Id);
        return card;
    }

    private void AttachDragHandlers(UiCard card, string configurationId)
    {
        // WPF-UI cards contain buttons and templates that may mark mouse events handled.
        // Listen to handled preview events so dragging still starts from the card body.
        card.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler((_, e) =>
            {
                _dragCandidate = IsActionElement(e.OriginalSource as DependencyObject) ? null : card;
                _dragStart = e.GetPosition(card);
                _dragInProgress = false;
            }), true);
        card.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent,
            new MouseButtonEventHandler((_, _) =>
            {
                _dragCandidate = null;
                _dragInProgress = false;
            }), true);
        card.AddHandler(UIElement.PreviewMouseMoveEvent,
            new MouseEventHandler((_, e) =>
            {
                if (_dragCandidate != card || _dragInProgress || e.LeftButton != MouseButtonState.Pressed) return;
                if ((e.GetPosition(card) - _dragStart).Length < SystemParameters.MinimumHorizontalDragDistance) return;

                _dragInProgress = true;
                card.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.58, TimeSpan.FromMilliseconds(120)));
                try
                {
                    var data = new DataObject(ConfigurationDragFormat, configurationId);
                    data.SetData(DataFormats.StringFormat, configurationId);
                    data.SetData(DataFormats.UnicodeText, configurationId);
                    DragDrop.DoDragDrop(card, data, DragDropEffects.Move);
                }
                finally
                {
                    card.BeginAnimation(OpacityProperty, null);
                    card.Opacity = 1;
                    _dragCandidate = null;
                    _dragInProgress = false;
                }
            }), true);
    }

    private void OpenEditor(BlendConfiguration? source, bool isNew)
    {
        var initial = source ?? new BlendConfiguration("new", UiText.Get("configurationName"), false, new BlendSettings { NormalizeContentWidth = true });
        try
        {
            var dialog = new ConfigurationEditorWindow(initial) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;
            if (isNew) _session.AddUserConfiguration(dialog.ConfigurationName, dialog.Settings);
            else if (!_session.RenameConfiguration(initial.Id, dialog.ConfigurationName) || !_session.UpdateConfiguration(initial.Id, dialog.Settings))
                ErrorText.Text = UiText.Get("configurationUpdateFailed");
        }
        catch (Exception exception)
        {
            // A malformed legacy configuration must not take down the shell.
            ErrorText.Text = $"{UiText.Get("configurationUpdateFailed")}: {exception.Message}";
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => OpenEditor(null, true);
    private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not UiCard card || IsActionElement(e.OriginalSource as DependencyObject)) return;
        _dragStart = e.GetPosition(card);
    }

    private static bool IsActionElement(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is UiButton) return true;
            source = source is Visual visual ? VisualTreeHelper.GetParent(visual) : LogicalTreeHelper.GetParent(source);
        }
        return false;
    }

    private static string? GetDraggedConfigurationId(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(ConfigurationDragFormat))
            return Convert.ToString(e.Data.GetData(ConfigurationDragFormat));
        if (e.Data.GetDataPresent(DataFormats.StringFormat))
            return Convert.ToString(e.Data.GetData(DataFormats.StringFormat));
        return e.Data.GetDataPresent(DataFormats.UnicodeText)
            ? Convert.ToString(e.Data.GetData(DataFormats.UnicodeText))
            : null;
    }

    private void Slot_DragOver(object sender, DragEventArgs e)
    {
        var id = GetDraggedConfigurationId(e);
        if (sender is UiCard card && id is not null && _session.GetConfiguration(id) is not null)
        {
            card.Opacity = 0.82;
            card.BorderBrush = (Brush)FindResource("AccentDarkBrush");
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Slot_Drop(object sender, DragEventArgs e)
    {
        if (sender is UiCard card && card.Tag is int target && GetDraggedConfigurationId(e) is string id)
            _session.AddToSlots(id, target);
        if (sender is UiCard dropped) ResetSlotVisual(dropped);
        e.Handled = true;
    }
    private void ResetSlotVisual(UiCard card) { card.Opacity = 1; card.BorderBrush = (Brush)FindResource(_session.SlotConfigurations.ElementAtOrDefault((int)card.Tag!) is null ? "BorderBrush" : "AccentBrush"); }
    private UiButton MakeIconAction(string symbol, string tooltip, bool enabled)
    {
        var button = new UiButton { Content = new Wpf.Ui.Controls.SymbolIcon { Symbol = Enum.Parse<Wpf.Ui.Controls.SymbolRegular>(symbol), FontSize = 16 }, ToolTip = tooltip, IsEnabled = enabled, Appearance = ControlAppearance.Transparent, Padding = new Thickness(7), Margin = new Thickness(2, 0, 0, 0) };
        AutomationProperties.SetName(button, tooltip);
        return button;
    }
    private Border CreateIntensityBar(int intensity) => new() { Height = 4, Background = (Brush)FindResource("BorderBrush"), CornerRadius = new CornerRadius(2), Child = new Border { Width = Math.Max(4, 140 * (intensity + 1) / 10d), HorizontalAlignment = HorizontalAlignment.Left, Background = IntensityBrush(intensity), CornerRadius = new CornerRadius(2) } };
    private Brush IntensityBrush(int intensity) { var stops = new[] { Colors.ForestGreen, Colors.YellowGreen, Colors.Gold, Colors.Orange, Colors.OrangeRed, Colors.Firebrick }; return new SolidColorBrush(stops[(int)Math.Round(Math.Clamp(intensity, 0, 9) / 9d * (stops.Length - 1))]); }
    private string FeatureSummary(BlendConfiguration configuration) => string.Join(" · ", configuration.EnabledFeatures.Select(feature => feature switch { BlendFeature.NormalizeContentWidth => UiText.Get("normalizeWidth"), BlendFeature.Flatten => UiText.Get("flatten"), BlendFeature.Watermark => UiText.Get("watermark"), BlendFeature.PixelNoise => UiText.Get("noise"), _ => feature.ToString() }).DefaultIfEmpty(UiText.Get("noFeatures")));
}
