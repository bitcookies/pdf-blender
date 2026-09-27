using System.Windows.Media;
using System.IO;

namespace PDFBlender.App;

public enum UiNotificationKind { Info, Success, Warning, Error, Progress }

public sealed class UiNotificationService
{
    private readonly UiNotificationHost _host;
    private readonly AppSession _session;
    private readonly MediaPlayer _toastPlayer = new();

    internal UiNotificationService(UiNotificationHost host, AppSession session)
    {
        _host = host;
        _session = session;
    }

    public Guid Show(UiNotificationKind kind, string title, string message, bool autoDismiss = true)
    {
        var id = Guid.NewGuid();
        Dispatch(() =>
        {
            _host.Show(id, kind, title, message, autoDismiss);
            if (kind != UiNotificationKind.Progress && _session.Preferences.ToastSoundEnabled)
                PlayToastSound();
        });
        return id;
    }

    public void UpdateProgress(Guid id, string message, double fraction) => Dispatch(() => _host.UpdateProgress(id, message, fraction));
    public void Dismiss(Guid id) => Dispatch(() => _host.Dismiss(id));
    public void Clear() => Dispatch(_host.Clear);

    private void PlayToastSound()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "sounds", "toast.mp3");
            if (!File.Exists(path)) return;
            _toastPlayer.Open(new System.Uri(path));
            _toastPlayer.Play();
        }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }

    private static void Dispatch(Action action)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }
}
