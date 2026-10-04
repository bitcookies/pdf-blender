using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PDFBlender.Core.Models;
using UiCard = Wpf.Ui.Controls.CardControl;
using UiButton = Wpf.Ui.Controls.Button;
using UiMenuItem = Wpf.Ui.Controls.MenuItem;
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
        ConfigurationsHeader.Text = UiText.Get("blendConfigurations");
        AddButtonText.Text = UiText.Get("addConfiguration");
        EmptyText.Text = _session.AllConfigurations.Count == 0 ? UiText.Get("noConfigurations") : string.Empty;
        ErrorText.Text = string.Empty;
        if (ChildContent.Visibility != Visibility.Visible)
        {
            BuildSlots();
            BuildCards(_session.AllConfigurations);
        }
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
        var card = new UiCard
        {
            Margin = new Thickness(index == 0 ? 0 : 4, 0, index == 4 ? 0 : 4, 0),
            Padding = new Thickness(8),
            Height = 72,
            MinHeight = 72,
            MaxHeight = 72,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Background = (Brush)FindResource(configuration is null ? "SurfaceMutedBrush" : "CardBrush"),
            BorderBrush = configuration is null ? (Brush)FindResource("BorderBrush") : ConfigurationPalette.GetBrush(configuration.ColorKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            AllowDrop = true,
            Tag = index,
            ToolTip = string.Format(UiText.Get("slotDropHint"), index + 1)
        };

        if (configuration is null)
        {
            var empty = new Grid { VerticalAlignment = VerticalAlignment.Center };
            empty.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            empty.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            empty.Children.Add(new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = Wpf.Ui.Controls.SymbolRegular.Add24,
                FontSize = 17,
                Foreground = (Brush)FindResource("MutedTextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            var hint = new TextBlock
            {
                Text = string.Format(UiText.Get("slotDropHint"), index + 1),
                FontSize = 9.5,
                Foreground = (Brush)FindResource("MutedTextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0)
            };
            Grid.SetRow(hint, 1);
            empty.Children.Add(hint);
            card.Content = empty;
        }
        else
        {
            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var icon = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = ConfigurationIconCatalog.GetSymbol(configuration.IconKey),
                FontSize = 20,
                Foreground = ConfigurationPalette.GetBrush(configuration.ColorKey),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            root.Children.Add(icon);
            var details = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 3, 0) };
            details.Children.Add(new TextBlock { Text = configuration.Name, FontSize = 10.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            details.Children.Add(new TextBlock { Text = $"ID {configuration.ShortId} · {UiText.Get(ConfigurationIntensity.LabelKey(configuration.Intensity))}", FontSize = 9, Foreground = (Brush)FindResource("MutedTextBrush"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 2, 0, 0) });
            Grid.SetColumn(details, 1);
            root.Children.Add(details);
            var remove = MakeIconAction("Dismiss16", UiText.Get("remove"), true);
            remove.Padding = new Thickness(2);
            remove.Margin = new Thickness(0);
            remove.Click += (_, _) => _session.RemoveFromSlots(configuration.Id);
            Grid.SetColumn(remove, 2);
            root.Children.Add(remove);
            card.Content = root;
            AttachDragHandlers(card, configuration.Id);
        }

        card.AddHandler(UIElement.PreviewDragOverEvent, new DragEventHandler(Slot_DragOver), true);
        card.AddHandler(UIElement.DragLeaveEvent, new DragEventHandler((_, _) => ResetSlotVisual(card)), true);
        card.AddHandler(UIElement.PreviewDropEvent, new DragEventHandler(Slot_Drop), true);
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
            Margin = new Thickness(0, 0, 6, 8),
            Padding = new Thickness(8),
            Height = 104,
            MinHeight = 104,
            MaxHeight = 104,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Background = (Brush)FindResource("CardBrush"),
            BorderBrush = ConfigurationPalette.GetBrush(configuration.ColorKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            ToolTip = UiText.Get("dragToSlots"),
            Focusable = false
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var icon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = ConfigurationIconCatalog.GetSymbol(configuration.IconKey),
            FontSize = 21,
            Foreground = ConfigurationPalette.GetBrush(configuration.ColorKey),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Grid.SetRowSpan(icon, 3);
        grid.Children.Add(icon);

        var name = new TextBlock { Text = configuration.Name, FontSize = 12, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 2, 0) };
        Grid.SetColumn(name, 1);
        grid.Children.Add(name);

        var menuButton = MakeIconAction("MoreHorizontal24", UiText.Get("more"), true);
        menuButton.Padding = new Thickness(4);
        menuButton.Margin = new Thickness(0);
        Grid.SetColumn(menuButton, 2);
        Grid.SetRow(menuButton, 0);
        grid.Children.Add(menuButton);
        menuButton.ContextMenu = CreateConfigurationMenu(menuButton, configuration);
        menuButton.Click += (_, e) =>
        {
            if (menuButton.ContextMenu is { } menu)
            {
                menu.PlacementTarget = menuButton;
                menu.Placement = PlacementMode.Bottom;
                menu.IsOpen = true;
            }
            e.Handled = true;
        };

        var summary = new TextBlock { Text = FeatureSummary(configuration), Foreground = (Brush)FindResource("MutedTextBrush"), FontSize = 9.5, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 2, 0) };
        Grid.SetColumn(summary, 1);
        Grid.SetRow(summary, 1);
        grid.Children.Add(summary);

        var meta = new TextBlock { Text = $"ID {configuration.ShortId} · {UiText.Get(ConfigurationIntensity.LabelKey(configuration.Intensity))}", Foreground = ConfigurationPalette.GetBrush(configuration.ColorKey), FontSize = 9, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(5, 4, 2, 0) };
        Grid.SetColumn(meta, 1);
        Grid.SetRow(meta, 2);
        grid.Children.Add(meta);

        card.Content = grid;
        AttachDragHandlers(card, configuration.Id);
        return card;
    }

    private ContextMenu CreateConfigurationMenu(UiButton target, BlendConfiguration configuration)
    {
        var menu = new ContextMenu { PlacementTarget = target, Placement = PlacementMode.Bottom, StaysOpen = false };
        var edit = new UiMenuItem { Header = UiText.Get("edit"), Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Edit16 }, IsEnabled = !configuration.IsLocked };
        edit.Click += (_, _) => OpenEditor(configuration, false);
        var lockItem = new UiMenuItem { Header = configuration.IsLocked ? UiText.Get("unlock") : UiText.Get("lock"), Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = configuration.IsLocked ? Wpf.Ui.Controls.SymbolRegular.LockOpen16 : Wpf.Ui.Controls.SymbolRegular.LockClosed16 } };
        lockItem.Click += (_, _) => _session.SetConfigurationLocked(configuration.Id, !configuration.IsLocked);
        var delete = new UiMenuItem { Header = UiText.Get("delete"), Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Delete16 }, IsEnabled = !configuration.IsLocked };
        delete.Click += async (_, _) =>
        {
            if (configuration.IsLocked) return;
            var confirmed = await _dialogs.ShowConfirmationAsync(UiText.Get("delete"), UiText.Get("deleteConfigurationConfirm"), UiText.Get("delete"), UiText.Get("cancel"), Wpf.Ui.Controls.SymbolRegular.Delete24);
            if (confirmed) _session.DeleteConfiguration(configuration.Id);
        };
        menu.Items.Add(edit);
        menu.Items.Add(lockItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(delete);
        return menu;
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
            var scale = new ScaleTransform(1, 1);
            card.RenderTransformOrigin = new Point(0.5, 0.5);
            card.RenderTransform = scale;
            var animation = new DoubleAnimation(1, 0.97, TimeSpan.FromMilliseconds(150));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            card.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.62, TimeSpan.FromMilliseconds(150)));
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
                card.RenderTransform = null;
                card.Opacity = 1;
                _dragCandidate = null;
                _dragInProgress = false;
            }
        }), true);
    }

    private void OpenEditor(BlendConfiguration? source, bool isNew)
    {
        Action parentBack = isNew
            ? () => ShowChild(new ConfigurationCreatePage(_session, () => OpenEditor(null, true), OpenPresets, ShowList))
            : ShowList;
        ShowChild(new ConfigurationEditorPage(_session, source, isNew, parentBack, SaveEditor, ShowList));
    }

    private void OpenPresets()
    {
        ShowChild(new ConfigurationPresetPage(_session, presetId =>
        {
            _session.AddPresetConfiguration(presetId);
            ShowList();
        }, () => ShowChild(new ConfigurationCreatePage(_session, () => OpenEditor(null, true), OpenPresets, ShowList)), ShowList));
    }

    private void SaveEditor(BlendConfiguration source, bool isNew, string name, BlendSettings settings, string colorKey, string iconKey)
    {
        if (isNew)
            _session.AddUserConfiguration(name, settings, colorKey, iconKey);
        else if (!_session.RenameConfiguration(source.Id, name) || !_session.UpdateConfiguration(source.Id, settings, colorKey, iconKey))
        {
            ErrorText.Text = UiText.Get("configurationUpdateFailed");
            return;
        }
        ShowList();
    }

    private void ShowChild(Page page)
    {
        ListScroller.Visibility = Visibility.Collapsed;
        ChildContent.Visibility = Visibility.Visible;
        ChildContent.Content = page;
    }

    private void ShowList()
    {
        ChildContent.Content = null;
        ChildContent.Visibility = Visibility.Collapsed;
        ListScroller.Visibility = Visibility.Visible;
        RefreshFromSession();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) =>
        ShowChild(new ConfigurationCreatePage(_session, () => OpenEditor(null, true), OpenPresets, ShowList));

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
        var button = new UiButton { Content = new Wpf.Ui.Controls.SymbolIcon { Symbol = Enum.Parse<Wpf.Ui.Controls.SymbolRegular>(symbol), FontSize = 16 }, ToolTip = tooltip, IsEnabled = enabled, Appearance = ControlAppearance.Transparent, Padding = new Thickness(7), Margin = new Thickness(2, 0, 0, 0), Cursor = Cursors.Hand };
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
