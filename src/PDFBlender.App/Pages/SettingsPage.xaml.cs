using System.Windows;
using System.Windows.Controls;

namespace PDFBlender.App.Pages;

public partial class SettingsPage : Page
{
    private readonly AppSession _session;
    private bool _initializing;

    internal SettingsPage(AppSession session)
    {
        _session = session;
        InitializeComponent();
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        _initializing = true;
        UiText.SetLanguage(_session.Preferences.Language);
        TitleText.Text = UiText.Get("settings");
        SubtitleText.Text = UiText.Get("settingsSubtitle");
        AppearanceHeader.Text = UiText.Get("appearance");
        ThemeLabel.Text = UiText.Get("theme");
        ThemeHint.Text = UiText.Get("themeHint");
        LanguageLabel.Text = UiText.Get("language");
        LanguageHint.Text = UiText.Get("languageHint");
        ThemeOptions.ItemsSource = new[] { UiText.Get("system"), UiText.Get("light"), UiText.Get("dark") };
        LanguageOptions.ItemsSource = new[] { UiText.Get("system"), "English", "简体中文" };
        ThemeOptions.SelectedIndex = (int)_session.Preferences.Theme;
        LanguageOptions.SelectedIndex = (int)_session.Preferences.Language;
        _initializing = false;
    }

    private void ThemeOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing && ThemeOptions.SelectedIndex >= 0)
            _session.ApplyPreferences(theme: (ThemePreference)ThemeOptions.SelectedIndex);
    }

    private void LanguageOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing && LanguageOptions.SelectedIndex >= 0)
            _session.ApplyPreferences(language: (UiLanguage)LanguageOptions.SelectedIndex);
    }
}
