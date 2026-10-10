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
    private readonly SingleInstanceGuard _singleInstance = new(SingleInstanceMutexName);

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var preferences = AppPreferencesStore.Load();
        if (!preferences.AllowMultipleInstances && !_singleInstance.TryAcquire())
        {
            await SingleInstanceGuard.ActivateExistingWindowAsync();
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance.Dispose();
        base.OnExit(e);
    }
}

