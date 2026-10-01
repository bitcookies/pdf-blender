using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace PDFBlender.App.Pages;

public partial class SettingsPage : Page
{
    private static readonly string[] SoundFiles = ["toast-1.mp3", "toast-2.mp3", "toast-3.mp3", "toast-4.mp3", "toast-5.mp3"];
    private readonly AppSession _session;
    private readonly DialogService _dialogs;
    private readonly UiNotificationService _notifications;
    private ScrollBar? _verticalScrollBar;
    private DispatcherTimer? _scrollBarHideTimer;
    private bool _scrollBarEventsAttached;
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
        ToastSoundOptions.ItemsSource = Enumerable.Range(1, SoundFiles.Length).Select(index => UiText.Get($"sound{index}")).ToArray();
        ToastSoundOptions.SelectedIndex = Array.FindIndex(SoundFiles, sound => string.Equals(sound, _session.Preferences.ToastSound, StringComparison.OrdinalIgnoreCase));
        if (ToastSoundOptions.SelectedIndex < 0) ToastSoundOptions.SelectedIndex = 0;
        TestToastSoundButton.ToolTip = UiText.Get("testSound");
        WindowsNotificationsLabel.Text = UiText.Get("windowsNotifications");
        WindowsNotificationsHint.Text = UiText.Get("windowsNotificationsHint");
        WindowsNotificationsToggle.IsChecked = _session.Preferences.WindowsNotificationsEnabled;
        UpdateNotificationSoundControls();
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
        if (!_initializing && ToastSoundOptions.SelectedIndex >= 0 && ToastSoundOptions.SelectedIndex < SoundFiles.Length)
            _session.ApplyPreferences(toastSound: SoundFiles[ToastSoundOptions.SelectedIndex]);
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

    private void SettingsScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsScrollViewer.ApplyTemplate();
        _verticalScrollBar = FindVisualChild<ScrollBar>(SettingsScrollViewer);
        if (_verticalScrollBar is null) return;

        if (!_scrollBarEventsAttached)
        {
            _verticalScrollBar.MouseEnter += ScrollBar_MouseEnter;
            _verticalScrollBar.MouseLeave += ScrollBar_MouseLeave;
            _scrollBarEventsAttached = true;
        }

        _scrollBarHideTimer ??= new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _scrollBarHideTimer.Tick -= ScrollBarHideTimer_Tick;
        _scrollBarHideTimer.Tick += ScrollBarHideTimer_Tick;
        UpdateScrollBarVisibility();
    }

    private void SettingsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) => ShowScrollBar();

    private void SettingsScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) => ShowScrollBar();

    private void ScrollBar_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) => ShowScrollBar(false);

    private void ScrollBar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => ScheduleScrollBarHide();

    private void ShowScrollBar(bool scheduleHide = true)
    {
        if (_verticalScrollBar is null || SettingsScrollViewer.ScrollableHeight <= 0)
        {
            if (_verticalScrollBar is not null) _verticalScrollBar.Opacity = 0;
            return;
        }

        _verticalScrollBar.BeginAnimation(UIElement.OpacityProperty, null);
        _verticalScrollBar.Opacity = 1;
        if (scheduleHide) ScheduleScrollBarHide();
    }

    private void ScheduleScrollBarHide()
    {
        if (_verticalScrollBar is null || SettingsScrollViewer.ScrollableHeight <= 0 || _scrollBarHideTimer is null) return;
        _scrollBarHideTimer.Stop();
        _scrollBarHideTimer.Start();
    }

    private void ScrollBarHideTimer_Tick(object? sender, EventArgs e)
    {
        _scrollBarHideTimer?.Stop();
        if (_verticalScrollBar is null || _verticalScrollBar.IsMouseOver) return;

        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300))
        {
            FillBehavior = FillBehavior.HoldEnd
        };
        _verticalScrollBar.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    private void UpdateScrollBarVisibility()
    {
        if (SettingsScrollViewer.ScrollableHeight > 0) ShowScrollBar();
        else if (_verticalScrollBar is not null) _verticalScrollBar.Opacity = 0;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) return match;
            var nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }

        return null;
    }

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
        (Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 5, 0, 0)).ToString(4);
}
