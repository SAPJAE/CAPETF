using System.IO;

namespace CAPETF.Desktop;

internal static class SyntheticTradingComposition
{
    public static string DefaultExecutionStorePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CAPETF",
            "synthetic-executions.json");

    public static SyntheticTradingHostCoordinator CreateCoordinator(
        ICapitalTradingGateway gateway,
        string executionStorePath,
        Func<bool> isDemoTradingSession,
        Func<CancellationToken, Task<IReadOnlyList<CapitalOpenPosition>>> getOpenPositions,
        ISyntheticExecutionClock? clock = null,
        Func<DateTimeOffset>? utcNow = null,
        Func<string>? currentAccountId = null)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(isDemoTradingSession);
        ArgumentNullException.ThrowIfNull(getOpenPositions);

        var coordinatorClock = utcNow ?? (clock is null
            ? () => DateTimeOffset.UtcNow
            : () => clock.UtcNow);
        return new SyntheticTradingHostCoordinator(
            new SyntheticBasketExecutionService(gateway, clock),
            new SyntheticExecutionStore(executionStorePath),
            new SyntheticPositionReconciler(),
            isDemoTradingSession,
            getOpenPositions,
            coordinatorClock,
            currentAccountId);
    }
}
