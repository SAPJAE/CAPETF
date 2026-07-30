namespace CAPETF.Desktop;

internal sealed class SyntheticTradingWindowLifecycleCoordinator
{
    private static readonly TimeSpan DefaultPreDispatchWait = TimeSpan.FromSeconds(2);
    private readonly object _gate = new();
    private readonly TimeSpan _preDispatchWait;
    private OperationState? _activeOperation;
    private bool _closeRequested;
    private bool _closeAuthorized;

    public SyntheticTradingWindowLifecycleCoordinator(TimeSpan? preDispatchWait = null)
    {
        _preDispatchWait = preDispatchWait ?? DefaultPreDispatchWait;
        if (_preDispatchWait < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(preDispatchWait));
        }
    }

    public TrackedOperation BeginOperation()
    {
        lock (_gate)
        {
            if (_closeRequested)
            {
                throw new InvalidOperationException("The trading window is closing.");
            }
            if (_activeOperation is { IsComplete: false })
            {
                throw new InvalidOperationException("A tracked trading operation is already running.");
            }

            _activeOperation = new OperationState();
            return new TrackedOperation(this, _activeOperation);
        }
    }

    public bool RequestClose(Action cancelPendingOperations, Action authorizeDeferredClose)
    {
        ArgumentNullException.ThrowIfNull(cancelPendingOperations);
        ArgumentNullException.ThrowIfNull(authorizeDeferredClose);

        OperationState? operation;
        lock (_gate)
        {
            if (_closeAuthorized)
            {
                return true;
            }
            if (_closeRequested)
            {
                return false;
            }

            _closeRequested = true;
            operation = _activeOperation;
        }

        cancelPendingOperations();

        lock (_gate)
        {
            if (operation is null || operation.IsComplete)
            {
                _closeAuthorized = true;
                return true;
            }
        }

        _ = AuthorizeWhenSafeAsync(operation, authorizeDeferredClose);
        return false;
    }

    private async Task AuthorizeWhenSafeAsync(OperationState operation, Action authorizeDeferredClose)
    {
        if (!IsMutationDispatched(operation))
        {
            using var timeout = new CancellationTokenSource();
            var delay = Task.Delay(_preDispatchWait, timeout.Token);
            var completed = await Task.WhenAny(operation.Completion.Task, operation.MutationDispatched.Task, delay);
            if (completed != delay)
            {
                timeout.Cancel();
            }

            lock (_gate)
            {
                if (operation.IsComplete)
                {
                    _closeAuthorized = true;
                }
                else if (!operation.IsMutationDispatched)
                {
                    // A later dispatch mark will now fail before entering the mutation gateway.
                    _closeAuthorized = true;
                }
            }
        }

        if (IsMutationDispatched(operation) && !operation.IsComplete)
        {
            await operation.Completion.Task;
        }

        lock (_gate)
        {
            _closeAuthorized = true;
        }
        authorizeDeferredClose();
    }

    private bool IsMutationDispatched(OperationState operation)
    {
        lock (_gate)
        {
            return operation.IsMutationDispatched;
        }
    }

    private void MarkMutationDispatched(OperationState operation)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(operation, _activeOperation) || operation.IsComplete)
            {
                throw new InvalidOperationException("The tracked trading operation is no longer active.");
            }
            if (_closeAuthorized)
            {
                throw new OperationCanceledException("Window shutdown completed before mutation dispatch.");
            }

            operation.IsMutationDispatched = true;
            operation.MutationDispatched.TrySetResult();
        }
    }

    private void Track(OperationState operation, Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        lock (_gate)
        {
            if (!ReferenceEquals(operation, _activeOperation) || operation.IsTracked)
            {
                throw new InvalidOperationException("The trading operation cannot be tracked more than once.");
            }
            operation.IsTracked = true;
        }

        _ = task.ContinueWith(
            static (_, state) => ((OperationState)state!).Completion.TrySetResult(),
            operation,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    internal sealed class TrackedOperation
    {
        private readonly SyntheticTradingWindowLifecycleCoordinator _owner;
        private readonly OperationState _state;

        internal TrackedOperation(SyntheticTradingWindowLifecycleCoordinator owner, OperationState state)
        {
            _owner = owner;
            _state = state;
        }

        public void MarkMutationDispatched() => _owner.MarkMutationDispatched(_state);

        public void Track(Task task) => _owner.Track(_state, task);
    }

    internal sealed class OperationState
    {
        public TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource MutationDispatched { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsTracked { get; set; }
        public bool IsMutationDispatched { get; set; }
        public bool IsComplete => Completion.Task.IsCompleted;
    }
}
