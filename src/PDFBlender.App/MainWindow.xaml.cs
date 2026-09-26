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
    private readonly UiNotificationService _notifications;
    private HomePage? _homePage;
    private ConfigurationPage? _configurationPage;
    private SettingsPage? _settingsPage;
    private AboutPage? _aboutPage;
    private string _currentPage = "home";
    private bool _railSelectionIndicatorReady;
    private bool _railIndicatorLayoutDirty = true;

    public MainWindow()
    {
        InitializeComponent();
        _notifications = new UiNotificationService(AppNotificationHost);
        ApplicationThemeManager.Apply(this);
        ApplyTheme();
        ApplyLanguage();
        NavigateTo("home");
        _session.Changed += Session_Changed;
        Loaded += (_, _) => Dispatcher.BeginInvoke(() => UpdateRailSelectionIndicator(false), DispatcherPriority.Loaded);
    }

    private void HomeNavigationItem_Click(object sender, RoutedEventArgs e) => NavigateTo("home");
    private void ConfigurationNavigationItem_Click(object sender, RoutedEventArgs e) => NavigateTo("configuration");
    private void SettingsNavigationItem_Click(object sender, RoutedEventArgs e) => NavigateTo("settings");
    private void AboutNavigationItem_Click(object sender, RoutedEventArgs e) => NavigateTo("about");

    private void NavigateTo(string page)
    {
        _currentPage = page;
        ContentFrame.Content = page switch
        {
            "configuration" => _configurationPage ??= new ConfigurationPage(_session),
            "settings" => _settingsPage ??= new SettingsPage(_session),
            "about" => _aboutPage ??= new AboutPage(),
            _ => _homePage ??= new HomePage(_session, _notifications, () => NavigateTo("configuration"))
        };
        SetNavigationState(page);
        ShellTitleBar.Title = "PDF Blender";
        AnimateContent();
    }

    private void Session_Changed(object? sender, EventArgs e)
    {
        ApplyTheme();
        ApplyLanguage();
        SetNavigationState(_currentPage);
        _homePage?.RefreshFromSession();
        _configurationPage?.RefreshFromSession();
        _settingsPage?.RefreshFromSession();
        _aboutPage?.RefreshLanguage();
    }

    private void ApplyLanguage()
    {
        UiText.SetLanguage(_session.Preferences.Language);
        SetRailText(HomeNavigationButton, UiText.Get("home"));
        SetRailText(ConfigurationNavigationButton, UiText.Get("configuration"));
        SetRailText(SettingsNavigationButton, UiText.Get("settings"));
        SetRailText(AboutNavigationButton, UiText.Get("about"));
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

    private void SetNavigationState(string page)
    {
        SetRailButtonState(HomeNavigationButton, page == "home");
        SetRailButtonState(ConfigurationNavigationButton, page == "configuration");
        SetRailButtonState(SettingsNavigationButton, page == "settings");
        SetRailButtonState(AboutNavigationButton, page == "about");
        Dispatcher.BeginInvoke(() => UpdateRailSelectionIndicator(true), DispatcherPriority.Loaded);
    }

    private void RailRoot_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _railIndicatorLayoutDirty = true;
        Dispatcher.BeginInvoke(() => UpdateRailSelectionIndicator(false), DispatcherPriority.Loaded);
    }

    private void UpdateRailSelectionIndicator(bool animate)
    {
        var target = _currentPage switch
        {
            "configuration" => ConfigurationNavigationButton,
            "settings" => SettingsNavigationButton,
            "about" => AboutNavigationButton,
            _ => HomeNavigationButton
        };

        if (!target.IsVisible || target.ActualHeight <= 0 || RailSelectionIndicator.ActualHeight <= 0)
        {
            Dispatcher.BeginInvoke(() => UpdateRailSelectionIndicator(animate), DispatcherPriority.Loaded);
            return;
        }

        var targetTop = target.TranslatePoint(new Point(0, 0), RailRoot).Y;
        var targetY = Math.Max(0, targetTop + (target.ActualHeight - RailSelectionIndicator.ActualHeight) / 2);
        if (RailSelectionIndicator.RenderTransform is not TranslateTransform translate)
        {
            translate = new TranslateTransform();
            RailSelectionIndicator.RenderTransform = translate;
        }

        var currentY = translate.Y;
        translate.BeginAnimation(TranslateTransform.YProperty, null);
        if (!animate || !_railSelectionIndicatorReady || _railIndicatorLayoutDirty)
        {
            translate.Y = targetY;
            _railSelectionIndicatorReady = true;
            _railIndicatorLayoutDirty = false;
            return;
        }

        var animation = new DoubleAnimation(currentY, targetY, TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };
        animation.Completed += (_, _) => translate.Y = targetY;
        translate.BeginAnimation(TranslateTransform.YProperty, animation);
    }

    private static void SetRailButtonState(System.Windows.Controls.Button button, bool selected)
    {
        button.Tag = selected;
        button.Background = Brushes.Transparent;
        button.Foreground = selected
            ? (Brush)Application.Current.Resources["AccentBrush"]
            : (Brush)Application.Current.Resources["NavTextBrush"];

        // Keep the active rail marker explicit as a fallback for template/resource
        // reloads. This mirrors the reference app's independent indicator.
        button.ApplyTemplate();
        if (button.Template.FindName("ActiveSurface", button) is UIElement activeSurface)
        {
            activeSurface.BeginAnimation(UIElement.OpacityProperty, null);
            var animation = new DoubleAnimation(selected ? 1 : 0, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            };
            animation.Completed += (_, _) => activeSurface.Opacity = selected ? 1 : 0;
            activeSurface.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }

    private static void SetRailText(System.Windows.Controls.Button button, string text)
    {
        if (button.Content is StackPanel panel && panel.Children.OfType<System.Windows.Controls.TextBlock>().FirstOrDefault() is { } label)
            label.Text = text;
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
