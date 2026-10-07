using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using PDFBlender.App.Pages;
using Wpf.Ui.Appearance;
using Wpf.Ui.Animations;
using Wpf.Ui.Controls;

namespace PDFBlender.App;

public partial class MainWindow : FluentWindow
{
    private readonly AppSession _session = new();
    private readonly DialogService _dialogs;
    private readonly UiNotificationService _notifications;
    private HomePage? _homePage;
    private ConfigurationPage? _configurationPage;
    private SettingsPage? _settingsPage;
    private string _currentPage = "home";

    public MainWindow()
    {
        InitializeComponent();
        DpiChanged += MainWindow_DpiChanged;
        AddHandler(Keyboard.PreviewGotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnKeyboardFocus), true);
        _dialogs = new DialogService(RootDialogHost);
        _notifications = new UiNotificationService(AppNotificationHost, _session);
        ApplicationThemeManager.Apply(this);
        ApplyTheme();
        ApplyLanguage();
        NavigateTo("home");
        _session.Changed += Session_Changed;
        Loaded += MainWindow_Loaded;

    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        DpiSupport.RefreshVisuals(this);
        ApplyTitleBarButtonPalette();
        Dispatcher.BeginInvoke(() => SelectNavigationItem("home"), DispatcherPriority.Loaded);
    }

    private void ApplyTitleBarButtonPalette()
    {
        ShellTitleBar.ApplyTemplate();

        var normal = Application.Current.TryFindResource("TitleBarButtonsForegroundBrush") as Brush
            ?? Brushes.White;
        var hover = Application.Current.TryFindResource("TitleBarButtonsHoverBrush") as Brush
            ?? Brushes.Transparent;

        foreach (var name in new[]
        {
            "PART_MinimizeButton",
            "PART_MaximizeButton",
            "PART_RestoreButton",
            "PART_CloseButton",
            "PART_HelpButton"
        })
        {
            if (ShellTitleBar.Template?.FindName(name, ShellTitleBar) is not TitleBarButton button)
                continue;

            button.ButtonsForeground = normal;
            button.MouseOverButtonsForeground = normal;
            button.RenderButtonsForeground = normal;
            button.PressedForeground = normal;
            button.Background = Brushes.Transparent;
            button.MouseOverBackground = hover;
            button.PressedBackground = hover;
            button.BorderBrush = Brushes.Transparent;
            button.MouseOverBorderBrush = Brushes.Transparent;
            button.PressedBorderBrush = Brushes.Transparent;
        }
    }
    private void MainContentSurface_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var width = MainContentSurface.ActualWidth;
        var height = MainContentSurface.ActualHeight;
        if (width <= 0 || height <= 0)
        {
            MainContentSurface.Clip = null;
            return;
        }

        const double radius = 12;
        var r = Math.Min(radius, Math.Min(width, height));
        var geometry = new PathGeometry();
        var figure = new PathFigure
        {
            StartPoint = new Point(r, 0),
            IsClosed = true
        };
        figure.Segments.Add(new LineSegment(new Point(width, 0), true));
        figure.Segments.Add(new LineSegment(new Point(width, height), true));
        figure.Segments.Add(new LineSegment(new Point(0, height), true));
        figure.Segments.Add(new LineSegment(new Point(0, r), true));
        figure.Segments.Add(new ArcSegment(new Point(r, 0), new Size(r, r), 90, false, SweepDirection.Clockwise, true));
        geometry.Figures.Add(figure);
        MainContentSurface.Clip = geometry;
    }

    private void MainWindow_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        // WPF keeps dimensions in device-independent pixels; refresh only the visual tree.
        DpiSupport.RefreshVisuals(this);
        Dispatcher.BeginInvoke(() => ContentFrame.InvalidateMeasure(), DispatcherPriority.Render);
    }

    private void NavigationView_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.NavigationView navigationView
            || navigationView.SelectedItem is not Wpf.Ui.Controls.NavigationViewItem item)
            return;

        if (item.Tag is string page && !string.Equals(page, _currentPage, StringComparison.Ordinal))
            NavigateTo(page, synchronizeSelection: false);
    }

    private void NavigationItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.NavigationViewItem { Tag: string page })
        {
            if (string.Equals(page, _currentPage, StringComparison.Ordinal))
            {
                if (page == "configuration")
                    _configurationPage?.ReturnToList();
                return;
            }

            SelectNavigationItem(page);
            NavigateTo(page, synchronizeSelection: false);
        }
    }

    private void NavigationItem_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement item && e.NewSize.Width > 0 && e.NewSize.Height > 0)
            ApplyNavigationItemChrome(item);
    }

    private void NavigationItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement item)
            ApplyNavigationItemChrome(item);
    }

    private static void ApplyNavigationItemChrome(FrameworkElement item)
    {
        if (item is Wpf.Ui.Controls.NavigationViewItem navigationItem)
        {
            navigationItem.ApplyTemplate();
            navigationItem.ClipToBounds = false;
            ApplyNavigationItemForeground(navigationItem);
            navigationItem.MouseEnter -= NavigationItem_MouseEnter;
            navigationItem.MouseEnter += NavigationItem_MouseEnter;
            navigationItem.MouseLeave -= NavigationItem_MouseLeave;
            navigationItem.MouseLeave += NavigationItem_MouseLeave;
            if (navigationItem.Template?.FindName("MainBorder", navigationItem) is Border surface)
            {
                surface.SizeChanged -= NavigationSurface_SizeChanged;
                surface.SizeChanged += NavigationSurface_SizeChanged;
                surface.CornerRadius = new CornerRadius(8);
                ApplyNavigationSurfaceGeometry(surface);
                ApplyNavigationItemSurface(navigationItem, surface);
                ApplyNavigationItemIcon(navigationItem);
            }
        }
    }

    private static void NavigationSurface_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Border surface)
            ApplyNavigationSurfaceGeometry(surface);
    }

    private static void ApplyNavigationSurfaceGeometry(Border surface)
    {
        surface.CornerRadius = new CornerRadius(8);
        surface.Clip = null;
    }

    private static void NavigationItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.NavigationViewItem item)
        {
            ApplyNavigationItemForeground(item);
            ApplyNavigationItemSurface(item);
            ApplyNavigationItemIcon(item);
        }
    }

    private static void NavigationItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.NavigationViewItem item)
        {
            ApplyNavigationItemForeground(item);
            ApplyNavigationItemSurface(item);
            ApplyNavigationItemIcon(item);
        }
    }

    private static void ApplyNavigationItemForeground(Wpf.Ui.Controls.NavigationViewItem item)
    {
        var key = item.IsActive
            ? "NavigationViewItemForegroundLeftFluent"
            : item.IsMouseOver
                ? "NavigationViewItemForegroundPointerOver"
                : "NavigationViewItemForeground";

        if (Application.Current.TryFindResource(key) is Brush brush)
        {
            item.Foreground = brush;
            item.SetValue(System.Windows.Documents.TextElement.ForegroundProperty, brush);
            ApplyNavigationTextVisuals(item, brush);
        }
    }

    private static void ApplyNavigationTextVisuals(DependencyObject root, Brush foreground)
    {
        if (root is System.Windows.Controls.TextBlock textBlock)
            textBlock.Foreground = foreground;

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            ApplyNavigationTextVisuals(VisualTreeHelper.GetChild(root, index), foreground);
    }

    private static void ApplyNavigationItemIcon(Wpf.Ui.Controls.NavigationViewItem item)
    {
        if (item.Icon is not IconElement icon)
            return;

        var key = item.IsActive
            ? "NavigationViewItemForegroundLeftFluent"
            : "NavigationViewItemForeground";
        if (Application.Current.TryFindResource(key) is Brush brush)
            icon.Foreground = brush;
    }

    private static void OnKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (e.NewFocus is Control control)
            control.FocusVisualStyle = null;
    }

    private static void ApplyNavigationItemSurface(Wpf.Ui.Controls.NavigationViewItem item, Border? surface = null)
    {
        surface ??= item.Template?.FindName("MainBorder", item) as Border;
        if (surface is null)
            return;

        var key = item.IsMouseOver
            ? "NavigationViewItemBackgroundPointerOverLeftFluent"
            : item.IsActive
                ? "NavigationViewItemBackgroundSelectedLeftFluent"
                : null;

        surface.Background = key is null
            ? Brushes.Transparent
            : Application.Current.TryFindResource(key) as Brush ?? Brushes.Transparent;
        ApplyNavigationSurfaceGeometry(surface);
    }

    private void NavigateTo(string page, bool synchronizeSelection = true)
    {
        if (string.Equals(page, _currentPage, StringComparison.Ordinal) && ContentFrame.Content is not null)
        {
            if (synchronizeSelection)
                SelectNavigationItem(page);
            return;
        }

        _currentPage = page;
        ContentFrame.Content = page switch
        {
            "configuration" => _configurationPage ??= new ConfigurationPage(_session, _dialogs),
            "settings" => _settingsPage ??= new SettingsPage(_session, _dialogs, _notifications),
            _ => _homePage ??= new HomePage(_session, _notifications, () => NavigateTo("configuration"))
        };
        if (synchronizeSelection)
            SelectNavigationItem(page);
        ShellTitleBar.Title = "PDF Blender";
        AnimateContent();
    }

    private void Session_Changed(object? sender, EventArgs e)
    {
        ApplyTheme();
        ApplyLanguage();
        _homePage?.RefreshFromSession();
        _configurationPage?.RefreshFromSession();
        _settingsPage?.RefreshFromSession();
    }

    private void ApplyLanguage()
    {
        UiText.SetLanguage(_session.Preferences.Language);
        HomeNavigationItem.Content = UiText.Get("home");
        ConfigurationNavigationItem.Content = UiText.Get("configurationNavigation");
        SettingsNavigationItem.Content = UiText.Get("settings");
        ShellTitleBar.Title = "PDF Blender";
    }

    private void ApplyTheme()
    {
        var preference = _session.Preferences.Theme;
        var isDark = preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            _ => ThemeManager.SystemUsesLightTheme() is false
        };
        var applicationTheme = isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        var backdrop = isDark ? WindowBackdropType.None : WindowBackdropType.Mica;
        ApplicationThemeManager.Apply(applicationTheme, backdrop, true);
        WindowBackdropType = backdrop;
        var accent = (Color)ColorConverter.ConvertFromString(isDark ? "#E74856" : "#C9021E")!;
        ApplicationAccentColorManager.Apply(accent, accent, accent, accent);
        ThemeManager.ApplyPalette(isDark);
        var windowBrush = (Brush)Application.Current.Resources["WindowBackgroundBrush"];
        var shellBrush = (Brush)Application.Current.Resources["ShellChromeBrush"];
        var sidebarBrush = (Brush)Application.Current.Resources["SidebarBrush"];
        Background = windowBrush;
        RootLayout.Background = windowBrush;
        ShellTitleBar.Background = shellBrush;
        ShellSurfaceGrid.Background = shellBrush;
        ShellNavigationView.Background = sidebarBrush;
        MainContentSurface.Background = windowBrush;
        ContentFrame.Background = windowBrush;
        if (IsInitialized)
        {
            ApplyTitleBarButtonPalette();
            foreach (var navigationItem in ShellNavigationView.MenuItems
                         .Cast<object>()
                         .Concat(ShellNavigationView.FooterMenuItems.Cast<object>())
                         .OfType<Wpf.Ui.Controls.NavigationViewItem>())
            {
                ApplyNavigationItemChrome(navigationItem);
            }
        }
    }

    private void SelectNavigationItem(string page)
    {
        var items = ShellNavigationView.MenuItems
            .Cast<object>()
            .Concat(ShellNavigationView.FooterMenuItems.Cast<object>())
            .OfType<Wpf.Ui.Controls.NavigationViewItem>()
            .ToArray();
        var item = items.FirstOrDefault(candidate => string.Equals(candidate.Tag as string, page, StringComparison.Ordinal));
        if (item is null)
            return;

        foreach (var activeItem in items.Where(candidate => candidate.IsActive && !ReferenceEquals(candidate, item)))
            activeItem.Deactivate(ShellNavigationView);

        if (!item.IsActive)
            item.Activate(ShellNavigationView);

        foreach (var navigationItem in items)
            ApplyNavigationItemChrome(navigationItem);
    }

    private void AnimateContent()
    {
        var motion = UiMotionPolicy.Resolve();
        if (motion.EnterSlide <= TimeSpan.Zero)
        {
            ContentFrame.Opacity = 1;
            ContentFrame.RenderTransform = null;
            return;
        }

        TransitionAnimationProvider.ApplyTransition(
            ContentFrame,
            Transition.FadeInWithSlide,
            (int)motion.EnterSlide.TotalMilliseconds);
    }
}
