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
