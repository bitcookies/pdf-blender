using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PDFBlender.Core.Models;
using UiCard = Wpf.Ui.Controls.Card;
using UiButton = Wpf.Ui.Controls.Button;
using UiMenuItem = Wpf.Ui.Controls.MenuItem;
using ControlAppearance = Wpf.Ui.Controls.ControlAppearance;

namespace PDFBlender.App.Pages;

public partial class ConfigurationPage : Page
{
    private const string ConfigurationDragFormat = "PDFBlender.ConfigurationId";
    private const double CardHeight = 72;
    private const double CardContentMinHeight = CardHeight - 8;
    private const double ChildTransitionOffset = 18;
    private readonly AppSession _session;
    private readonly DialogService _dialogs;
    private Point _dragStart;
    private UiCard? _dragCandidate;
    private bool _dragInProgress;
    private bool _listTransitionPending;

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
        EmptyText.Visibility = _session.AllConfigurations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ErrorText.Text = string.Empty;
        if (ChildContent.Visibility != Visibility.Visible || _listTransitionPending)
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
        var card = CreateCardSurface(
            configuration,
            new Thickness(3),
            configuration is null
                ? (Brush)FindResource("SurfaceMutedBrush")
                : (Brush)FindResource("CardBrush"), useColorBorder: false);
        card.Tag = index;
        card.AllowDrop = true;
        card.ToolTip = string.Format(UiText.Get("slotDropHint"), index + 1);

        if (configuration is null)
        {
            var empty = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinHeight = CardContentMinHeight
            };
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = Wpf.Ui.Controls.SymbolRegular.Add24,
                FontSize = 20,
                Foreground = (Brush)FindResource("MutedTextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            stack.Children.Add(new TextBlock
            {
                Text = string.Format(UiText.Get("slotDropHint"), index + 1),
                FontSize = 9.5,
                Foreground = (Brush)FindResource("MutedTextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });
            empty.Children.Add(stack);
            card.Content = empty;
        }
        else
        {
            card.Content = CreateConfigurationContent(configuration, isSlot: true);
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
        foreach (var configuration in configurations)
            ConfigurationCardsPanel.Children.Add(CreateConfigurationCard(configuration));
    }

    private UiCard CreateConfigurationCard(BlendConfiguration configuration)
    {
        var used = _session.SlotIds.Any(id => string.Equals(id, configuration.Id, StringComparison.OrdinalIgnoreCase));
        var card = CreateCardSurface(
            configuration,
            new Thickness(3, 0, 3, 6),
            (Brush)FindResource("CardBrush"), useColorBorder: false);
        card.ToolTip = used ? UiText.Get("configurationInUse") : UiText.Get("dragToSlots");
        card.Opacity = used ? 0.52 : 1;
        card.Content = CreateConfigurationContent(configuration, isSlot: false, used);
        if (!used)
            AttachDragHandlers(card, configuration.Id);
        return card;
    }

    private UiCard CreateCardSurface(BlendConfiguration? configuration, Thickness margin, Brush background, bool useColorBorder)
    {
        return new UiCard
        {
            Margin = margin,
            Padding = new Thickness(3),
            Height = CardHeight,
            MinHeight = CardHeight,
            MaxHeight = CardHeight,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Background = background,
            BorderBrush = configuration != null && useColorBorder && !configuration.IsLocked ? ConfigurationPalette.GetBrush(configuration.ColorKey) : (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            Focusable = false
        };
    }
    private Grid CreateConfigurationContent(BlendConfiguration configuration, bool isSlot, bool used = false)
    {
        var foreground = (Brush)FindResource("TextBrush");
        var root = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            MinHeight = CardContentMinHeight
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var icon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = ConfigurationIconCatalog.GetSymbol(configuration.IconKey),
            FontSize = 16,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var iconCircle = new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = ConfigurationPalette.GetBrush(configuration.ColorKey),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = icon
        };
        Grid.SetColumn(iconCircle, 0);
        Grid.SetRow(iconCircle, 0);
        root.Children.Add(iconCircle);

        var action = MakeStatusAction(configuration, isSlot, used, foreground);
        Grid.SetColumn(action, 1);
        Grid.SetRow(action, 0);
        root.Children.Add(action);

        var footer = new Grid
        {
            Margin = new Thickness(0, 2, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var name = new TextBlock
        {
            Text = configuration.Name,
            FontSize = 12,
            Foreground = foreground,
            FontWeight = FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 110,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(name, 0);
        footer.Children.Add(name);

        var id = CreatePill(
            configuration.ShortId ?? string.Empty,
            foreground,
            new Thickness(3, 0, 3, 0),
            7.5,
            new Thickness(4, 0, 0, 0));
        Grid.SetColumn(id, 1);
        footer.Children.Add(id);

        Grid.SetColumnSpan(footer, 2);
        Grid.SetRow(footer, 1);
        root.Children.Add(footer);
        return root;
    }
    private static Border CreatePill(string text, Brush foreground, Thickness padding, double fontSize, Thickness margin)
    {
        var color = foreground is SolidColorBrush solid ? solid.Color : Colors.Black;
        var border = new Border
        {
            Background = (Brush)Application.Current.FindResource("PillBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = padding,
            Margin = margin,
            VerticalAlignment = VerticalAlignment.Center
        };
        border.Child = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Cascadia Mono"),
            FontSize = fontSize,
            Foreground = foreground,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        return border;
    }
    private Border MakeStatusAction(BlendConfiguration configuration, bool isSlot, bool used, Brush foreground)
    {
        var symbol = isSlot
            ? "ArrowDown16"
            : used
                ? "Checkmark16"
                : configuration.IsLocked
                    ? "LockClosed16"
                    : "MoreHorizontal24";
        var tooltip = isSlot
            ? UiText.Get("takeDown")
            : used
                ? UiText.Get("configurationInUse")
                : configuration.IsLocked
                    ? UiText.Get("unlock")
                    : UiText.Get("more");

        var button = MakeIconAction(symbol, tooltip, isSlot || !used);
        button.Width = 20;
        button.Height = 20;
        button.MinWidth = 0;
        button.MinHeight = 0;
        button.Padding = new Thickness(0);
        button.Margin = new Thickness(0);
        button.Background = Brushes.Transparent;
        button.BorderBrush = Brushes.Transparent;
        button.BorderThickness = new Thickness(0);
        button.Foreground = foreground;
        button.CornerRadius = new CornerRadius(10);
        button.HorizontalAlignment = HorizontalAlignment.Center;
        button.VerticalAlignment = VerticalAlignment.Center;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.FocusVisualStyle = null;
        if (button.Content is Wpf.Ui.Controls.SymbolIcon statusIcon)
        {
            statusIcon.FontSize = 12;
            statusIcon.Foreground = foreground;
        }

        if (isSlot)
        {
            button.Click += (_, _) => _session.RemoveFromSlots(configuration.Id);
        }
        else if (!used)
        {
            button.ContextMenu = CreateConfigurationMenu(button, configuration);
            button.Click += (_, e) =>
            {
                if (button.ContextMenu is { } menu)
                {
                    menu.PlacementTarget = button;
                    menu.Placement = PlacementMode.Bottom;
                    menu.IsOpen = true;
                }
                e.Handled = true;
            };
        }
        var actionBorderBrush = configuration.IsLocked && !isSlot
            ? new SolidColorBrush(Color.FromRgb(0x6F, 0x6F, 0x6F))
            : (Brush)FindResource("BorderBrush");

        // Match the Home PDF remove control with a separate circular surface.
        return new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            Background = (Brush)FindResource("CardBrush"),
            BorderBrush = actionBorderBrush,
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Child = button
        };
    }
    private ContextMenu CreateConfigurationMenu(UiButton target, BlendConfiguration configuration)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = target,
            Placement = PlacementMode.Bottom,
            StaysOpen = false
        };
        var edit = new UiMenuItem
        {
            Header = UiText.Get("edit"),
            Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Edit16 },
            IsEnabled = !configuration.IsLocked
        };
        edit.Click += (_, _) => Dispatcher.BeginInvoke(new Action(() => OpenEditor(configuration, false)), DispatcherPriority.ContextIdle);
        var copy = new UiMenuItem
        {
            Header = UiText.Get("copy"),
            Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Copy16 }
        };
        copy.Click += (_, _) => _session.CopyConfiguration(configuration.Id);
        var lockItem = new UiMenuItem
        {
            Header = configuration.IsLocked ? UiText.Get("unlock") : UiText.Get("lock"),
            Icon = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = configuration.IsLocked
                    ? Wpf.Ui.Controls.SymbolRegular.LockOpen16
                    : Wpf.Ui.Controls.SymbolRegular.LockClosed16
            }
        };
        lockItem.Click += (_, _) => _session.SetConfigurationLocked(configuration.Id, !configuration.IsLocked);
        var delete = new UiMenuItem
        {
            Header = UiText.Get("delete"),
            Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Delete16 },
            IsEnabled = !configuration.IsLocked
        };
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
        menu.Items.Add(edit);
        menu.Items.Add(copy);
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
        Action parentBack = ShowList;
        ShowChild(new ConfigurationEditorPage(_session, _dialogs, source, isNew, parentBack, SaveEditor, ShowList));
    }

    private void OpenPresets()
    {
        ShowChild(new ConfigurationPresetPage(_session, presetId =>
        {
            _session.AddPresetConfiguration(presetId);
            ShowList();
        }, ShowList, ShowList));
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
        if (_listTransitionPending)
            return;

        _listTransitionPending = true;
        ListScroller.Visibility = Visibility.Collapsed;
        ListScroller.IsHitTestVisible = false;
        ChildContent.Visibility = Visibility.Visible;
        ChildContent.IsHitTestVisible = false;
        ChildContent.Content = page;

        var motion = UiMotionPolicy.Resolve();
        var offset = motion.Offset > 0 ? ChildTransitionOffset : 0;
        SetTransitionState(ChildContent, offset, 0);

        AnimateTransition(
            ChildContent,
            offset,
            0,
            0,
            1,
            motion.EnterSlide,
            () =>
            {
                ChildContent.IsHitTestVisible = true;
                _listTransitionPending = false;
            });
    }

    public void ReturnToList() => ShowList();

    private void ShowList()
    {
        if (_listTransitionPending)
            return;

        _listTransitionPending = true;
        ListScroller.Visibility = Visibility.Visible;
        ListScroller.IsHitTestVisible = false;
        ChildContent.IsHitTestVisible = false;
        RefreshFromSession();

        var motion = UiMotionPolicy.Resolve();
        var offset = motion.Offset > 0 ? ChildTransitionOffset : 0;
        SetTransitionState(ListScroller, -offset, 0);

        AnimateTransition(
            ChildContent,
            0,
            offset,
            1,
            0,
            motion.EnterSlide,
            null);
        AnimateTransition(
            ListScroller,
            -offset,
            0,
            0,
            1,
            motion.EnterSlide,
            CompleteListTransition);
    }

    private void CompleteListTransition()
    {
        ListScroller.Opacity = 1;
        ListScroller.IsHitTestVisible = true;
        ChildContent.Visibility = Visibility.Collapsed;
        ChildContent.Opacity = 1;
        ChildContent.IsHitTestVisible = false;

        // Keep the child attached until the routed BreadcrumbBar event and
        // the transition callback have both completed.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ChildContent.Content = null;
            _listTransitionPending = false;
        }), DispatcherPriority.ApplicationIdle);
    }

    private static void SetTransitionState(FrameworkElement element, double x, double opacity)
    {
        element.RenderTransform = new TranslateTransform(x, 0);
        element.Opacity = opacity;
    }

    private static void AnimateTransition(
        FrameworkElement element,
        double fromX,
        double toX,
        double fromOpacity,
        double toOpacity,
        TimeSpan duration,
        Action? completed)
    {
        if (duration <= TimeSpan.Zero)
        {
            element.RenderTransform = new TranslateTransform(toX, 0);
            element.Opacity = toOpacity;
            completed?.Invoke();
            return;
        }

        var transform = element.RenderTransform as TranslateTransform ?? new TranslateTransform();
        element.RenderTransform = transform;
        transform.X = fromX;
        element.Opacity = fromOpacity;

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var slide = new DoubleAnimation(fromX, toX, duration)
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.HoldEnd
        };
        var opacity = new DoubleAnimation(fromOpacity, toOpacity, duration)
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.HoldEnd
        };
        opacity.Completed += (_, _) =>
        {
            transform.BeginAnimation(TranslateTransform.XProperty, null);
            element.BeginAnimation(UIElement.OpacityProperty, null);
            transform.X = toX;
            element.Opacity = toOpacity;
            completed?.Invoke();
        };
        transform.BeginAnimation(TranslateTransform.XProperty, slide);
        element.BeginAnimation(UIElement.OpacityProperty, opacity);
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        const double menuWidth = 120;
        var menu = new ContextMenu
        {
            PlacementTarget = AddButton,
            Placement = PlacementMode.Bottom,
            HorizontalOffset = 0,
            Width = menuWidth,
            MinWidth = menuWidth,
            MaxWidth = menuWidth,
            StaysOpen = false,
            Focusable = false,
            FocusVisualStyle = null
        };
        var custom = new UiMenuItem
        {
            Header = UiText.Get("newCustomConfiguration"),
            Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.TextBoxSettings24 },
            Focusable = false,
            IsTabStop = false,
            FocusVisualStyle = null
        };
        custom.Click += (_, _) => Dispatcher.BeginInvoke(new Action(() => OpenEditor(null, true)), DispatcherPriority.ContextIdle);
        var preset = new UiMenuItem
        {
            Header = UiText.Get("newPresetConfiguration"),
            Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.WindowSettings20 },
            Focusable = false,
            IsTabStop = false,
            FocusVisualStyle = null
        };
        preset.Click += (_, _) => Dispatcher.BeginInvoke(new Action(OpenPresets), DispatcherPriority.ContextIdle);
        menu.Items.Add(custom);
        menu.Items.Add(preset);
        menu.IsOpen = true;
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
        if (sender is UiCard dropped)
            ResetSlotVisual(dropped);
        e.Handled = true;
    }

    private void ResetSlotVisual(UiCard card)
    {
        var configuration = card.Tag is int index ? _session.SlotConfigurations.ElementAtOrDefault(index) : null;
        card.BorderBrush = (Brush)FindResource("BorderBrush");
        card.BorderThickness = new Thickness(1);
        card.Background = configuration is null ? (Brush)FindResource("SurfaceMutedBrush") : (Brush)FindResource("CardBrush");
        card.Opacity = 1;
    }

    private UiButton MakeIconAction(string symbol, string tooltip, bool enabled)
    {
        var parsed = Enum.TryParse<Wpf.Ui.Controls.SymbolRegular>(symbol, out var value)
            ? value
            : Wpf.Ui.Controls.SymbolRegular.Add24;
        var button = new UiButton
        {
            Content = new Wpf.Ui.Controls.SymbolIcon { Symbol = parsed, FontSize = 16 },
            ToolTip = tooltip,
            IsEnabled = enabled,
            Appearance = ControlAppearance.Transparent,
            Padding = new Thickness(7),
            Margin = new Thickness(2, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        AutomationProperties.SetName(button, tooltip);
        return button;
    }
}
