using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PDFBlender.App;

internal sealed class SingleInstanceGuard : IDisposable
{
    private readonly string _mutexName;
    private Mutex? _mutex;

    public SingleInstanceGuard(string mutexName) => _mutexName = mutexName;

    public bool TryAcquire()
    {
        if (_mutex is not null) return true;
        try
        {
            var mutex = new Mutex(true, _mutexName, out var createdNew);
            if (!createdNew)
            {
                mutex.Dispose();
                return false;
            }

            _mutex = mutex;
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static async Task ActivateExistingWindowAsync()
    {
        using var current = Process.GetCurrentProcess();
        // A second launch can arrive before the first instance has created its window.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var processes = Process.GetProcessesByName(current.ProcessName);
            try
            {
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.Id == current.Id || process.SessionId != current.SessionId
                            || !string.Equals(process.MainModule?.FileName, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var handle = process.MainWindowHandle;
                        if (handle == IntPtr.Zero)
                            continue;

                        // Restore minimized windows without changing maximized windows.
                        if (IsIconic(handle))
                            ShowWindowAsync(handle, 9); // SW_RESTORE

                        if (SetForegroundWindow(handle))
                            return;
                    }
                    catch (InvalidOperationException) { } // The other instance may have exited.
                    catch (Win32Exception) { }
                }
            }
            finally
            {
                foreach (var process in processes)
                    process.Dispose();
            }

            await Task.Delay(100);
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(IntPtr window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);

    public void Dispose()
    {
        if (_mutex is null) return;
        try { _mutex.ReleaseMutex(); }
        catch (ApplicationException) { }
        finally
        {
            _mutex.Dispose();
            _mutex = null;
        }
    }
}
