namespace CAPETF.Desktop;

internal static class TerminalOperationExecution
{
    public static async Task<bool> RunAsync(
        Func<CancellationToken, Task> action,
        CancellationToken lifetimeToken,
        Action onCompleted,
        Action<Exception> onFailure)
    {
        if (lifetimeToken.IsCancellationRequested) return false;

        try
        {
            await action(lifetimeToken);
            lifetimeToken.ThrowIfCancellationRequested();
            onCompleted();
            return true;
        }
        catch (OperationCanceledException) when (lifetimeToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception) when (lifetimeToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            onFailure(ex);
            return false;
        }
    }

    public static async Task WrapStreamingStartAsync(
        Func<Task> startStreaming,
        string baseStatus,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await startStreaming();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{baseStatus} Live prices unavailable: {ex.Message}", ex);
        }
    }
}
