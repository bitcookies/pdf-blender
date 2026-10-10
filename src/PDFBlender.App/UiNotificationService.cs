using System.Windows.Media;
using System.IO;
using System.Windows.Threading;
using FormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using FormsToolTipIcon = System.Windows.Forms.ToolTipIcon;
using DrawingIcon = System.Drawing.Icon;
using DrawingSystemIcons = System.Drawing.SystemIcons;

namespace PDFBlender.App;

public enum UiNotificationKind { Info, Success, Warning, Error, Progress }

public sealed class UiNotificationService : IDisposable
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
            if (_session.Preferences.WindowsNotificationsEnabled
                && kind is UiNotificationKind.Success or UiNotificationKind.Error)
                ShowWindowsNotification(kind, title, message);
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
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "sounds", ToastSoundCatalog.Normalize(_session.Preferences.ToastSound));
            if (!File.Exists(path)) return;
            _toastPlayer.Open(new System.Uri(path));
            _toastPlayer.Play();
        }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }

    private void ShowWindowsNotification(UiNotificationKind kind, string title, string message)
    {
        try
        {
            if (_windowsNotifier is null)
            {
                var resource = System.Windows.Application.GetResourceStream(
                    new Uri("pack://application:,,,/PDFBlender.App;component/Assets/logo.ico"));
                using var stream = resource?.Stream;
                using var embeddedIcon = stream is not null ? new DrawingIcon(stream) : null;
                _windowsNotifier = new FormsNotifyIcon
                {
                    Icon = (DrawingIcon)(embeddedIcon ?? DrawingSystemIcons.Application).Clone(),
                    Text = "PDF Blender",
                    Visible = false
                };
            }

            _windowsNotifier.Visible = true;
            _windowsNotifier.ShowBalloonTip(5000, title, message,
                kind == UiNotificationKind.Error ? FormsToolTipIcon.Error : FormsToolTipIcon.None);
            _windowsNotifierHideTimer.Stop();
            _windowsNotifierHideTimer.Start();
        }
        catch (Exception) { }
    }

    public void Dispose()
    {
        _windowsNotifierHideTimer.Stop();
        var icon = _windowsNotifier?.Icon;
        _windowsNotifier?.Dispose();
        _windowsNotifier = null;
        icon?.Dispose();
        _toastPlayer.Close();
    }

    private static void Dispatch(Action action)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }
}
