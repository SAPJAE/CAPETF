namespace CAPETF.Desktop;

internal sealed class WindowLifetime : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private int _closing;

    public CancellationToken Token => _cancellation.Token;
    public bool IsClosing => Volatile.Read(ref _closing) != 0;

    public bool BeginClosing()
    {
        if (Interlocked.Exchange(ref _closing, 1) != 0) return false;
        _cancellation.Cancel();
        return true;
    }

    public bool TryApply(Action action)
    {
        if (IsClosing) return false;
        action();
        return !IsClosing;
    }

    public void Dispose()
    {
        BeginClosing();
        _cancellation.Dispose();
    }
}
