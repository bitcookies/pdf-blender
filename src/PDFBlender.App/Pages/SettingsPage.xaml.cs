using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace PDFBlender.App.Pages;

public partial class SettingsPage : Page
{
    private readonly AppSession _session;
    private readonly DialogService _dialogs;
    private readonly UiNotificationService _notifications;
    private bool _initializing;

    internal SettingsPage(AppSession session, DialogService dialogs, UiNotificationService notifications)
    {
        _session = session;
        _dialogs = dialogs;
        _notifications = notifications;
        InitializeComponent();
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        _initializing = true;
        UiText.SetLanguage(_session.Preferences.Language);
        TitleText.Text = UiText.Get("settings");
        SubtitleText.Text = UiText.Get("settingsSubtitle");
        AppearanceHeader.Text = UiText.Get("appearanceAndLanguage");
        StorageHeader.Text = UiText.Get("storageAndRuntime");
        SoundHeader.Text = UiText.Get("soundAndNotifications");
        AboutHeader.Text = UiText.Get("about");
        ThemeLabel.Text = UiText.Get("theme");
        ThemeHint.Text = UiText.Get("themeHint");
        LanguageLabel.Text = UiText.Get("language");
        LanguageHint.Text = UiText.Get("languageHint");
        SettingsFileLabel.Text = UiText.Get("settingsFile");
        SettingsFilePath.Text = AppPreferencesStore.FilePath;
        OpenFolderButtonText.Text = UiText.Get("openFolder");
        ToastSoundLabel.Text = UiText.Get("toastSound");
        ToastSoundHint.Text = UiText.Get("toastSoundHint");
        ToastSoundToggle.IsChecked = _session.Preferences.ToastSoundEnabled;
        RestorePresetsLabel.Text = UiText.Get("restorePresets");
        RestorePresetsHint.Text = UiText.Get("restorePresetsHint");
        ThemeOptions.ItemsSource = new[] { UiText.Get("system"), UiText.Get("light"), UiText.Get("dark") };
        LanguageOptions.ItemsSource = new[] { UiText.Get("system"), "English", "简体中文" };
        ThemeOptions.SelectedIndex = (int)_session.Preferences.Theme;
        LanguageOptions.SelectedIndex = (int)_session.Preferences.Language;
        RestorePresetsButtonText.Text = UiText.Get("restore");
        AboutProductName.Text = UiText.Get("appName");
        AboutCopyrightText.Text = UiText.Get("aboutCopyright");
        AboutVersionText.Text = $"v{GetApplicationVersion()}";
        AboutGithubLabel.Text = UiText.Get("aboutGithub");
        AboutLicenseLabel.Text = UiText.Get("aboutLicense");
        AboutFeedbackLabel.Text = UiText.Get("aboutFeedback");
        AboutDeveloperLabel.Text = UiText.Get("aboutDeveloper");
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

    private void ToastSoundToggle_Click(object sender, RoutedEventArgs e)
    {
        if (!_initializing)
            _session.ApplyPreferences(toastSoundEnabled: ToastSoundToggle.IsChecked == true);
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var folder = Path.GetDirectoryName(AppPreferencesStore.FilePath);
        if (string.IsNullOrWhiteSpace(folder)) return;
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{AppPreferencesStore.FilePath}\"",
            UseShellExecute = true
        });
    }

    private async void RestorePresetsButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = await _dialogs.ShowConfirmationAsync(
            UiText.Get("restorePresets"),
            UiText.Get("restorePresetsConfirm"),
            UiText.Get("restore"),
            UiText.Get("cancel"),
            SymbolRegular.ArrowSync24);
        if (!confirmed) return;

        var added = _session.RestorePresetsNow();
        _notifications.Show(
            added > 0 ? UiNotificationKind.Success : UiNotificationKind.Info,
            UiText.Get("restorePresets"),
            added > 0 ? string.Format(UiText.Get("restorePresetsSuccess"), added) : UiText.Get("restorePresetsNone"));
    }

    private void AboutSummaryButton_Click(object sender, RoutedEventArgs e)
    {
        var expanded = AboutDetails.Visibility == Visibility.Visible;
        AboutDetails.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        AboutExpandIcon.Symbol = expanded ? SymbolRegular.ChevronDown24 : SymbolRegular.ChevronUp24;
    }

    private void AboutLinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url } && Uri.TryCreate(url, UriKind.Absolute, out _))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static string GetApplicationVersion() =>
        (Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 5, 0, 0)).ToString(4);
}
