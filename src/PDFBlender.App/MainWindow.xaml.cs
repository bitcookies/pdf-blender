using System.Windows;
using System.Windows.Controls;
using PDFBlender.App.Pages;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PDFBlender.App;

public partial class MainWindow : FluentWindow
{
    private readonly AppSession _session = new();
    private HomePage? _homePage;
    private SettingsPage? _settingsPage;
    private AboutPage? _aboutPage;

    public MainWindow()
    {
        InitializeComponent();
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
            _ => _homePage ??= new HomePage(_session)
        };

        HomeNavigationItem.IsActive = page == "home";
        SettingsNavigationItem.IsActive = page == "settings";
        AboutNavigationItem.IsActive = page == "about";
        ShellTitleBar.Title = page switch
        {
            "settings" => UiText.Get("settings"),
            "about" => UiText.Get("about"),
            _ => "PDF Blender"
        };
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
        HomeNavigationItem.Content = UiText.Get("home");
        SettingsNavigationItem.Content = UiText.Get("settings");
        AboutNavigationItem.Content = UiText.Get("about");
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
        var theme = isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(theme);
        ThemeManager.ApplyPalette(isDark);
        Background = (System.Windows.Media.Brush)Application.Current.Resources["WindowBackgroundBrush"];
    }
}
