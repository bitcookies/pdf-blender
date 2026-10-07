using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        AllowMultipleInstancesLabel.Text = UiText.Get("allowMultipleInstances");
        AllowMultipleInstancesHint.Text = UiText.Get("allowMultipleInstancesHint");
        AllowMultipleInstancesToggle.IsChecked = _session.Preferences.AllowMultipleInstances;
        BannerNotificationsLabel.Text = UiText.Get("bannerNotifications");
        BannerNotificationsHint.Text = UiText.Get("bannerNotificationsHint");
        BannerNotificationsToggle.IsChecked = _session.Preferences.BannerNotificationsEnabled;
        BannerNotificationSoundLabel.Text = UiText.Get("bannerNotificationSound");
        BannerNotificationSoundHint.Text = UiText.Get("bannerNotificationSoundHint");
        NotificationSoundLabel.Text = UiText.Get("notificationSound");
        ToastSoundToggle.IsChecked = _session.Preferences.ToastSoundEnabled;
        ToastSoundOptions.ItemsSource = ToastSoundCatalog.Options.Select(option => UiText.Get(option.TextKey)).ToArray();
        ToastSoundOptions.SelectedIndex = ToastSoundCatalog.Options.ToList().FindIndex(option => string.Equals(option.FileName, _session.Preferences.ToastSound, StringComparison.OrdinalIgnoreCase));
        if (ToastSoundOptions.SelectedIndex < 0) ToastSoundOptions.SelectedIndex = 0;
        TestToastSoundButton.ToolTip = UiText.Get("testSound");
        WindowsNotificationsLabel.Text = UiText.Get("windowsNotifications");
        WindowsNotificationsHint.Text = UiText.Get("windowsNotificationsHint");
        WindowsNotificationsToggle.IsChecked = _session.Preferences.WindowsNotificationsEnabled;
        UpdateNotificationSoundControls();
        ThemeOptions.ItemsSource = new[] { UiText.Get("system"), UiText.Get("light"), UiText.Get("dark") };
        LanguageOptions.ItemsSource = new[] { UiText.Get("system"), "English", "简体中文" };
        ThemeOptions.SelectedIndex = (int)_session.Preferences.Theme;
        LanguageOptions.SelectedIndex = (int)_session.Preferences.Language;
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
        {
            _session.ApplyPreferences(toastSoundEnabled: ToastSoundToggle.IsChecked == true);
            UpdateNotificationSoundControls();
        }
    }

    private void BannerNotificationsToggle_Click(object sender, RoutedEventArgs e)
    {
        if (!_initializing)
        {
            _session.ApplyPreferences(bannerNotificationsEnabled: BannerNotificationsToggle.IsChecked == true);
            UpdateNotificationSoundControls();
        }
    }

    private void ToastSoundOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing && ToastSoundOptions.SelectedIndex >= 0 && ToastSoundOptions.SelectedIndex < ToastSoundCatalog.Options.Count)
            _session.ApplyPreferences(toastSound: ToastSoundCatalog.Options[ToastSoundOptions.SelectedIndex].FileName);
    }

    private void WindowsNotificationsToggle_Click(object sender, RoutedEventArgs e)
    {
        if (!_initializing)
            _session.ApplyPreferences(windowsNotificationsEnabled: WindowsNotificationsToggle.IsChecked == true);
    }

    private void UpdateNotificationSoundControls()
    {
        var enabled = BannerNotificationsToggle.IsChecked == true;
        ToastSoundToggle.IsEnabled = enabled;
        ToastSoundOptions.IsEnabled = enabled && ToastSoundToggle.IsChecked == true;
        TestToastSoundButton.IsEnabled = ToastSoundOptions.IsEnabled;
    }

    private void TestToastSoundButton_Click(object sender, RoutedEventArgs e) => _notifications.PlayToastSound();
    private void AllowMultipleInstancesToggle_Click(object sender, RoutedEventArgs e)
    {
        if (!_initializing)
            _session.ApplyPreferences(allowMultipleInstances: AllowMultipleInstancesToggle.IsChecked == true);
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

    private void AboutSummaryButton_Click(object sender, RoutedEventArgs e)
    {
        var expanded = AboutDetails.Visibility == Visibility.Visible;
        AboutDetails.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        if (AboutExpandIcon.RenderTransform is RotateTransform rotate)
        {
            var animation = new DoubleAnimation
            {
                To = expanded ? 0 : 180,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            rotate.BeginAnimation(RotateTransform.AngleProperty, animation);
        }
    }

    private void AboutLinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url } && Uri.TryCreate(url, UriKind.Absolute, out _))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static string GetApplicationVersion() =>
        (Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 9, 0, 0)).ToString(4);
}
