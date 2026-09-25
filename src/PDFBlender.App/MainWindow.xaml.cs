using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PDFBlender.App.Pages;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PDFBlender.App;

public partial class MainWindow : FluentWindow
{
    private readonly AppSession _session = new();
    private readonly UiNotificationService _notifications;
    private HomePage? _homePage;
    private SettingsPage? _settingsPage;
    private AboutPage? _aboutPage;

    public MainWindow()
    {
        InitializeComponent();
        _notifications = new UiNotificationService(AppNotificationHost);
        ApplicationThemeManager.Apply(this);
        ApplyTheme();
        ApplyLanguage();
        NavigateTo("home");
        _session.Changed += Session_Changed;
    }

    private void HomeNavigationItem_Click(object sender, RoutedEventArgs e) => NavigateTo("home");
    private void SettingsNavigationItem_Click(object sender, RoutedEventArgs e) => NavigateTo("settings");
    private void AboutNavigationItem_Click(object sender, RoutedEventArgs e) => NavigateTo("about");

    private void NavigateTo(string page)
    {
        ContentFrame.Content = page switch
        {
            "settings" => _settingsPage ??= new SettingsPage(_session),
            "about" => _aboutPage ??= new AboutPage(),
            _ => _homePage ??= new HomePage(_session, _notifications)
        };
        SetNavigationState(page);
        ShellTitleBar.Title = page switch
        {
            "settings" => UiText.Get("settings"),
            "about" => UiText.Get("about"),
            _ => UiText.Get("appName")
        };
        AnimateContent();
    }

    private void Session_Changed(object? sender, EventArgs e)
    {
        ApplyTheme();
        ApplyLanguage();
        _homePage?.RefreshFromSession();
        _settingsPage?.RefreshFromSession();
        _aboutPage?.RefreshLanguage();
    }

    private void ApplyLanguage()
    {
        UiText.SetLanguage(_session.Preferences.Language);
        SetRailText(HomeNavigationButton, UiText.Get("home"));
        SetRailText(SettingsNavigationButton, UiText.Get("settings"));
        SetRailText(AboutNavigationButton, UiText.Get("about"));
        ShellTitleBar.Title = UiText.Get("appName");
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
        SetRailButtonState(SettingsNavigationButton, page == "settings");
        SetRailButtonState(AboutNavigationButton, page == "about");
    }

    private static void SetRailButtonState(System.Windows.Controls.Button button, bool selected)
    {
        button.Background = selected
            ? (Brush)Application.Current.Resources["AccentSoftBrush"]
            : Brushes.Transparent;
        button.Foreground = selected
            ? (Brush)Application.Current.Resources["AccentBrush"]
            : (Brush)Application.Current.Resources["MutedTextBrush"];
    }

    private static void SetRailText(System.Windows.Controls.Button button, string text)
    {
        if (button.Content is StackPanel panel && panel.Children.OfType<System.Windows.Controls.TextBlock>().FirstOrDefault() is { } label)
            label.Text = text;
    }

    private void AnimateContent()
    {
        ContentFrame.Opacity = 0;
        var offset = new TranslateTransform(-6, 0);
        ContentFrame.RenderTransform = offset;
        var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slide = new DoubleAnimation(-6, 0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ContentFrame.BeginAnimation(UIElement.OpacityProperty, animation);
        offset.BeginAnimation(TranslateTransform.XProperty, slide);
    }
}
