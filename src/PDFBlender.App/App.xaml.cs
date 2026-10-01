using System.Configuration;
using System.Data;
using System.Windows;

namespace PDFBlender.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string SingleInstanceMutexName = "PDFBlender.SingleInstance.CurrentUser";
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var preferences = AppPreferencesStore.Load();
        if (!preferences.AllowMultipleInstances && !TryAcquireSingleInstanceMutex())
        {
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ReleaseSingleInstanceMutex();
        base.OnExit(e);
    }

    private bool TryAcquireSingleInstanceMutex()
    {
        if (_singleInstanceMutex is not null)
            return true;

        try
        {
            var mutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
            if (!createdNew)
            {
                mutex.Dispose();
                return false;
            }

            _singleInstanceMutex = mutex;
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void ReleaseSingleInstanceMutex()
    {
        if (_singleInstanceMutex is null)
            return;

        try { _singleInstanceMutex.ReleaseMutex(); }
        catch (ApplicationException) { }
        finally
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }
}

