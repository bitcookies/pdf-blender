using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    private AboutPage? _aboutPage;
    private string _currentPage = "home";

    public MainWindow()
    {
        InitializeComponent();
        _dialogs = new DialogService(RootDialogHost);
        _notifications = new UiNotificationService(AppNotificationHost, _session);
        ApplicationThemeManager.Apply(this);
        ApplyTheme();
        ApplyLanguage();
        NavigateTo("home");
        _session.Changed += Session_Changed;
        Loaded += (_, _) => Dispatcher.BeginInvoke(() => SelectNavigationItem("home"), DispatcherPriority.Loaded);
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
            navigationItem.MouseEnter -= NavigationItem_MouseEnter;
            navigationItem.MouseEnter += NavigationItem_MouseEnter;
            navigationItem.MouseLeave -= NavigationItem_MouseLeave;
            navigationItem.MouseLeave += NavigationItem_MouseLeave;
            if (navigationItem.Template?.FindName("MainBorder", navigationItem) is Border surface)
            {
                surface.CornerRadius = new CornerRadius(8);
                ApplyNavigationItemSurface(navigationItem, surface);
            }
        }
    }

    private static void NavigationItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.NavigationViewItem item)
            ApplyNavigationItemSurface(item);
    }

    private static void NavigationItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.NavigationViewItem item)
            ApplyNavigationItemSurface(item);
    }

    private static void ApplyNavigationItemSurface(Wpf.Ui.Controls.NavigationViewItem item, Border? surface = null)
    {
        surface ??= item.Template?.FindName("MainBorder", item) as Border;
        if (surface is null)
            return;

        var key = item.IsActive
            ? "NavigationViewItemBackgroundSelectedLeftFluent"
            : item.IsMouseOver
                ? "NavigationViewItemBackgroundPointerOverLeftFluent"
                : null;

        surface.Background = key is null
            ? Brushes.Transparent
            : Application.Current.TryFindResource(key) as Brush ?? Brushes.Transparent;
        surface.CornerRadius = new CornerRadius(8);
    }

    private void NavigateTo(string page, bool synchronizeSelection = true)
    {
        _currentPage = page;
        ContentFrame.Content = page switch
        {
            "configuration" => _configurationPage ??= new ConfigurationPage(_session, _dialogs),
            "settings" => _settingsPage ??= new SettingsPage(_session, _dialogs, _notifications),
            "about" => _aboutPage ??= new AboutPage(),
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
        _aboutPage?.RefreshLanguage();
    }

    private void ApplyLanguage()
    {
        UiText.SetLanguage(_session.Preferences.Language);
        HomeNavigationItem.Content = UiText.Get("home");
        ConfigurationNavigationItem.Content = UiText.Get("configuration");
        SettingsNavigationItem.Content = UiText.Get("settings");
        AboutNavigationItem.Content = UiText.Get("about");
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
        ApplicationThemeManager.Apply(isDark ? ApplicationTheme.Dark : ApplicationTheme.Light);
        var accent = (Color)ColorConverter.ConvertFromString("#FF7573")!;
        ApplicationAccentColorManager.Apply(accent, accent, accent, accent);
        ThemeManager.ApplyPalette(isDark);
        Background = (Brush)Application.Current.Resources["WindowBackgroundBrush"];
        if (IsInitialized)
        {
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
