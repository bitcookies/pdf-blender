using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PDFBlender.App.Pages;
using Wpf.Ui.Appearance;
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

    private void NavigationItem_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (sender is Wpf.Ui.Controls.NavigationViewItem item && item.Tag is string page)
            NavigateTo(page);
    }

    private void NavigationItem_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement item && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            item.Clip = new RectangleGeometry(new Rect(0, 0, e.NewSize.Width, e.NewSize.Height), 8, 8);
            item.ApplyTemplate();
            Dispatcher.BeginInvoke(() => ApplyNavigationItemCornerClip(item), DispatcherPriority.Loaded);
        }
    }

    private static void ApplyNavigationItemCornerClip(FrameworkElement item)
    {
        if (FindVisualChild<Border>(item, element => element.Name == "MainBorder") is not { } mainBorder)
            return;

        mainBorder.SizeChanged -= NavigationItemMainBorder_SizeChanged;
        mainBorder.SizeChanged += NavigationItemMainBorder_SizeChanged;
        SetRoundedClip(mainBorder);
    }

    private static void NavigationItemMainBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Border border)
            SetRoundedClip(border);
    }

    private static void SetRoundedClip(FrameworkElement element)
    {
        if (element.ActualWidth > 0 && element.ActualHeight > 0)
            element.Clip = new RectangleGeometry(new Rect(0, 0, element.ActualWidth, element.ActualHeight), 8, 8);
    }

    private static T? FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate)
        where T : FrameworkElement
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match && predicate(match))
                return match;

            if (FindVisualChild(child, predicate) is { } nested)
                return nested;
        }

        return null;
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
        SelectNavigationItem(_currentPage);
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
        ThemeManager.ApplyPalette(isDark);
        Background = (Brush)Application.Current.Resources["WindowBackgroundBrush"];
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

        foreach (var candidate in items)
        {
            candidate.Deactivate(ShellNavigationView);
            candidate.IsActive = false;
        }

        item.Activate(ShellNavigationView);
        item.IsActive = true;
        ApplyNavigationForeground(items, item);
    }

    private static void ApplyNavigationForeground(IEnumerable<Wpf.Ui.Controls.NavigationViewItem> items, Wpf.Ui.Controls.NavigationViewItem selected)
    {
        var resources = Application.Current.Resources;
        var activeBrush = resources["NavigationViewSelectionIndicatorForeground"] as Brush
            ?? resources["AccentBrush"] as Brush;
        var inactiveBrush = resources["NavTextBrush"] as Brush;

        if (activeBrush is null || inactiveBrush is null)
            return;

        foreach (var item in items)
            SetNavigationForeground(item, ReferenceEquals(item, selected) ? activeBrush : inactiveBrush);
    }

    private static void SetNavigationForeground(DependencyObject node, Brush brush)
    {
        if (node is SymbolIcon icon)
            icon.Foreground = brush;
        else if (node is System.Windows.Controls.TextBlock textBlock)
            textBlock.Foreground = brush;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            SetNavigationForeground(VisualTreeHelper.GetChild(node, i), brush);
    }

    private void AnimateContent()
    {
        var motion = UiMotionPolicy.Resolve();
        ContentFrame.Opacity = 0;
        var offset = new TranslateTransform(-motion.Offset, 0);
        ContentFrame.RenderTransform = offset;
        var animation = new DoubleAnimation(0, 1, motion.EnterOpacity)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slide = new DoubleAnimation(-motion.Offset, 0, motion.EnterSlide)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ContentFrame.BeginAnimation(UIElement.OpacityProperty, animation);
        offset.BeginAnimation(TranslateTransform.XProperty, slide);
    }
}
