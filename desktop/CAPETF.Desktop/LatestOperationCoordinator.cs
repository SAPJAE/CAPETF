namespace CAPETF.Desktop;

internal sealed class LatestOperationCoordinator : IDisposable
{
    private readonly object _gate = new();
    private CancellationTokenSource? _current;
    private int _generation;

    public OperationTicket Begin()
    {
        lock (_gate)
        {
            _current?.Cancel();
            _current?.Dispose();
            _current = new CancellationTokenSource();
            return new OperationTicket(++_generation, _current.Token);
        }
    }

    public bool IsCurrent(OperationTicket operation)
    {
        lock (_gate)
        {
            return operation.Generation == _generation && !operation.Token.IsCancellationRequested;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _current?.Cancel();
            _current?.Dispose();
            _current = null;
        }
    }
}

internal readonly record struct OperationTicket(int Generation, CancellationToken Token);
