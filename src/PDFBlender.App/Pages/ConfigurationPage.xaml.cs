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
        for (var index = 0; index < 5; index++)
        {
            SlotsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var slot = CreateSlot(_session.SlotConfigurations[index], index);
            Grid.SetColumn(slot, index);
            SlotsPanel.Children.Add(slot);
        }
    }

    private UiCard CreateSlot(BlendConfiguration? configuration, int index)
    {
        var border = configuration is null
            ? (Brush)FindResource("BorderBrush")
            : ConfigurationPalette.GetBrush(configuration.ColorKey);
        var card = new UiCard
        {
            Margin = new Thickness(index == 0 ? 0 : 5, 0, index == 4 ? 0 : 5, 0),
            Padding = new Thickness(9, 7, 9, 7),
            Height = 72,
            MinHeight = 72,
            MaxHeight = 72,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Background = (Brush)FindResource(configuration is null ? "SurfaceMutedBrush" : "CardBrush"),
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            AllowDrop = true,
            Tag = index,
            ToolTip = UiText.Get("slotDropHint")
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = new DockPanel();
        header.Children.Add(new TextBlock
        {
            Text = $"{index + 1}",
            Foreground = (Brush)FindResource("MutedTextBrush"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        if (configuration is not null)
        {
            var remove = MakeIconAction("Dismiss16", UiText.Get("remove"), true);
            remove.Padding = new Thickness(2);
            remove.Margin = new Thickness(0);
            remove.Click += (_, _) => _session.RemoveFromSlots(configuration.Id);
            DockPanel.SetDock(remove, Dock.Right);
            header.Children.Add(remove);
        }
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        if (configuration is null)
        {
            var empty = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            empty.Children.Add(new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Add24, FontSize = 15, Foreground = (Brush)FindResource("MutedTextBrush"), Margin = new Thickness(0, 0, 4, 0) });
            empty.Children.Add(new TextBlock { Text = UiText.Get("slotDropHint"), FontSize = 10, Foreground = (Brush)FindResource("MutedTextBrush"), VerticalAlignment = VerticalAlignment.Center });
            Grid.SetRow(empty, 1);
            root.Children.Add(empty);
        }
        else
        {
            var occupied = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            occupied.Children.Add(new TextBlock { Text = configuration.Name, FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("TextBrush"), TextTrimming = TextTrimming.CharacterEllipsis });
            occupied.Children.Add(new TextBlock { Text = $"ID {configuration.ShortId}  ·  {UiText.Get(ConfigurationIntensity.LabelKey(configuration.Intensity))}", FontSize = 9.5, Foreground = (Brush)FindResource("MutedTextBrush"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 2, 0, 0) });
            Grid.SetRow(occupied, 1);
            root.Children.Add(occupied);
        }

        card.Content = root;
        card.AddHandler(UIElement.PreviewDragOverEvent, new DragEventHandler(Slot_DragOver), true);
        card.AddHandler(UIElement.DragLeaveEvent, new DragEventHandler((_, _) => ResetSlotVisual(card)), true);
        card.AddHandler(UIElement.PreviewDropEvent, new DragEventHandler(Slot_Drop), true);
        if (configuration is not null) AttachDragHandlers(card, configuration.Id);
        return card;
    }

    private void BuildCards(IEnumerable<BlendConfiguration> configurations)
    {
        ConfigurationCardsPanel.Children.Clear();
        foreach (var configuration in configurations) ConfigurationCardsPanel.Children.Add(CreateCard(configuration));
    }

    private UiCard CreateCard(BlendConfiguration configuration)
    {
        var card = new UiCard
        {
            Margin = new Thickness(0, 0, 8, 10),
            Padding = new Thickness(12, 10, 8, 10),
            Height = 128,
            MinHeight = 128,
            MaxHeight = 128,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Background = (Brush)FindResource("CardBrush"),
            BorderBrush = ConfigurationPalette.GetBrush(configuration.ColorKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            ToolTip = UiText.Get("dragToSlots"),
            Focusable = false
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var details = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        details.Children.Add(new TextBlock { Text = configuration.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        details.Children.Add(new TextBlock { Text = FeatureSummary(configuration), Foreground = (Brush)FindResource("MutedTextBrush"), FontSize = 10.5, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, MaxHeight = 34, Margin = new Thickness(0, 5, 12, 0) });
        details.Children.Add(new TextBlock { Text = UiText.Get(ConfigurationIntensity.LabelKey(configuration.Intensity)), Foreground = ConfigurationPalette.GetBrush(configuration.ColorKey), FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0) });
        Grid.SetColumn(details, 0);
        grid.Children.Add(details);

        var actions = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(new TextBlock { Text = $"ID {configuration.ShortId}", Foreground = (Brush)FindResource("MutedTextBrush"), FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 3, 7) });
        var menuButton = MakeIconAction("MoreHorizontal24", UiText.Get("more"), true);
        var menu = new ContextMenu { Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        var editItem = new MenuItem { Header = UiText.Get("edit"), IsEnabled = !configuration.IsLocked };
        editItem.Click += (_, _) => OpenEditor(configuration, false);
        var lockItem = new MenuItem { Header = configuration.IsLocked ? UiText.Get("unlock") : UiText.Get("lock") };
        lockItem.Click += (_, _) => _session.SetConfigurationLocked(configuration.Id, !configuration.IsLocked);
        var deleteItem = new MenuItem { Header = UiText.Get("delete"), IsEnabled = !configuration.IsLocked };
        deleteItem.Click += async (_, _) =>
        {
            if (configuration.IsLocked) return;
            var confirmed = await _dialogs.ShowConfirmationAsync(UiText.Get("delete"), UiText.Get("deleteConfigurationConfirm"), UiText.Get("delete"), UiText.Get("cancel"), Wpf.Ui.Controls.SymbolRegular.Delete24);
            if (confirmed) _session.DeleteConfiguration(configuration.Id);
        };
        menu.Items.Add(editItem);
        menu.Items.Add(lockItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(deleteItem);
        menuButton.ContextMenu = menu;
        menuButton.Click += (_, _) => menu.IsOpen = true;
        actions.Children.Add(menuButton);
        Grid.SetColumn(actions, 1);
        grid.Children.Add(actions);
        card.Content = grid;
        AttachDragHandlers(card, configuration.Id);
        return card;
    }

    private void AttachDragHandlers(UiCard card, string configurationId)
    {
        card.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler((_, e) =>
        {
            _dragCandidate = IsActionElement(e.OriginalSource as DependencyObject) ? null : card;
            _dragStart = e.GetPosition(card);
            _dragInProgress = false;
        }), true);
        card.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler((_, _) =>
        {
            _dragCandidate = null;
            _dragInProgress = false;
        }), true);
        card.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler((_, e) =>
        {
            if (_dragCandidate != card || _dragInProgress || e.LeftButton != MouseButtonState.Pressed) return;
            if ((e.GetPosition(card) - _dragStart).Length < SystemParameters.MinimumHorizontalDragDistance) return;
            _dragInProgress = true;
            card.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.58, TimeSpan.FromMilliseconds(150)));
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
        var initial = source ?? new BlendConfiguration("new", UiText.Get("configurationName"), false, new BlendSettings { NormalizeContentWidth = true }, false, null, ConfigurationPalette.ForIndex(_session.AllConfigurations.Count));
        try
        {
            var dialog = new ConfigurationEditorWindow(initial) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;
            if (isNew)
                _session.AddUserConfiguration(dialog.ConfigurationName, dialog.Settings, dialog.SelectedColorKey);
            else if (!_session.RenameConfiguration(initial.Id, dialog.ConfigurationName) || !_session.UpdateConfiguration(initial.Id, dialog.Settings, dialog.SelectedColorKey))
                ErrorText.Text = UiText.Get("configurationUpdateFailed");
        }
        catch (Exception exception)
        {
            ErrorText.Text = $"{UiText.Get("configurationUpdateFailed")}: {exception.Message}";
        }
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var mode = await _dialogs.ShowConfigurationCreationAsync();
        if (mode is null) return;
        if (mode == ConfigurationCreationMode.Custom) { OpenEditor(null, true); return; }
        var presetId = await _dialogs.ShowPresetPickerAsync(_session.BuiltInConfigurations);
        if (!string.IsNullOrWhiteSpace(presetId)) _session.AddPresetConfiguration(presetId);
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
        if (e.Data.GetDataPresent(ConfigurationDragFormat)) return Convert.ToString(e.Data.GetData(ConfigurationDragFormat));
        if (e.Data.GetDataPresent(DataFormats.StringFormat)) return Convert.ToString(e.Data.GetData(DataFormats.StringFormat));
        return e.Data.GetDataPresent(DataFormats.UnicodeText) ? Convert.ToString(e.Data.GetData(DataFormats.UnicodeText)) : null;
    }

    private void Slot_DragOver(object sender, DragEventArgs e)
    {
        var id = GetDraggedConfigurationId(e);
        if (sender is UiCard card && id is not null && _session.GetConfiguration(id) is not null)
        {
            card.BorderBrush = (Brush)FindResource("AccentBrush");
            card.Background = (Brush)FindResource("AccentSoftBrush");
            e.Effects = DragDropEffects.Move;
        }
        else e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void Slot_Drop(object sender, DragEventArgs e)
    {
        if (sender is UiCard card && card.Tag is int target && GetDraggedConfigurationId(e) is string id) _session.AddToSlots(id, target);
        if (sender is UiCard dropped) ResetSlotVisual(dropped);
        e.Handled = true;
    }

    private void ResetSlotVisual(UiCard card)
    {
        var configuration = card.Tag is int index ? _session.SlotConfigurations.ElementAtOrDefault(index) : null;
        card.BorderBrush = configuration is null ? (Brush)FindResource("BorderBrush") : ConfigurationPalette.GetBrush(configuration.ColorKey);
        card.Background = (Brush)FindResource(configuration is null ? "SurfaceMutedBrush" : "CardBrush");
        card.Opacity = 1;
    }

    private UiButton MakeIconAction(string symbol, string tooltip, bool enabled)
    {
        var button = new UiButton { Content = new Wpf.Ui.Controls.SymbolIcon { Symbol = Enum.Parse<Wpf.Ui.Controls.SymbolRegular>(symbol), FontSize = 16 }, ToolTip = tooltip, IsEnabled = enabled, Appearance = ControlAppearance.Transparent, Padding = new Thickness(7), Margin = new Thickness(2, 0, 0, 0) };
        AutomationProperties.SetName(button, tooltip);
        return button;
    }

    private string FeatureSummary(BlendConfiguration configuration) => string.Join(" · ", configuration.EnabledFeatures.Select(feature => feature switch
    {
        BlendFeature.NormalizeContentWidth => UiText.Get("normalizeWidth"),
        BlendFeature.Flatten => UiText.Get("flatten"),
        BlendFeature.Watermark => UiText.Get("watermark"),
        BlendFeature.PixelNoise => UiText.Get("noise"),
        _ => feature.ToString()
    }).DefaultIfEmpty(UiText.Get("noFeatures")));
}
