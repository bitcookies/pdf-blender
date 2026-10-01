using System.Windows.Media;
using System.IO;
using System.Windows.Threading;
using FormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using FormsToolTipIcon = System.Windows.Forms.ToolTipIcon;
using DrawingIcon = System.Drawing.Icon;
using DrawingSystemIcons = System.Drawing.SystemIcons;

namespace PDFBlender.App;

public enum UiNotificationKind { Info, Success, Warning, Error, Progress }

public sealed class UiNotificationService
{
    private readonly UiNotificationHost _host;
    private readonly AppSession _session;
    private readonly MediaPlayer _toastPlayer = new();
    private FormsNotifyIcon? _windowsNotifier;
    private readonly DispatcherTimer _windowsNotifierHideTimer;

    internal UiNotificationService(UiNotificationHost host, AppSession session)
    {
        _host = host;
        _session = session;
        _windowsNotifierHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _windowsNotifierHideTimer.Tick += (_, _) =>
        {
            _windowsNotifierHideTimer.Stop();
            if (_windowsNotifier is not null) _windowsNotifier.Visible = false;
        };
    }

    public Guid Show(UiNotificationKind kind, string title, string message, bool autoDismiss = true)
    {
        var id = Guid.NewGuid();
        Dispatch(() =>
        {
            if (_session.Preferences.BannerNotificationsEnabled)
                _host.Show(id, kind, title, message, autoDismiss);
            if (_session.Preferences.BannerNotificationsEnabled && kind != UiNotificationKind.Progress && _session.Preferences.ToastSoundEnabled)
                PlayToastSound();
            if (_session.Preferences.WindowsNotificationsEnabled)
                ShowWindowsNotification(title, message);
        });
        return id;
    }

    public void UpdateProgress(Guid id, string message, double fraction) => Dispatch(() => _host.UpdateProgress(id, message, fraction));
    public void Dismiss(Guid id) => Dispatch(() => _host.Dismiss(id));
    public void Clear() => Dispatch(_host.Clear);

    public void PlayToastSound()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "sounds", _session.Preferences.ToastSound);
            if (!File.Exists(path)) return;
            _toastPlayer.Open(new System.Uri(path));
            _toastPlayer.Play();
        }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }

    private void ShowWindowsNotification(string title, string message)
    {
        try
        {
            if (_windowsNotifier is null)
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.ico");
                _windowsNotifier = new FormsNotifyIcon
                {
                    Icon = File.Exists(iconPath) ? new DrawingIcon(iconPath) : DrawingSystemIcons.Application,
                    Text = "PDF Blender",
                    Visible = false
                };
            }

            _windowsNotifier.Visible = true;
            _windowsNotifier.ShowBalloonTip(5000, title, message, FormsToolTipIcon.Info);
            _windowsNotifierHideTimer.Stop();
            _windowsNotifierHideTimer.Start();
        }
        catch (Exception) { }
    }

    private static void Dispatch(Action action)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }
}
