namespace CAPETF.Desktop;

public static class SyntheticStreamingSubscription
{
    public static async Task SubscribeAsync(
        CapitalStreamingClient client,
        CapitalSession session,
        SyntheticBasket basket,
        string timeframe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(basket);
        var epics = SyntheticTerminalWorkspace.StreamingEpics(basket);
        await client.SubscribeQuotesAsync(session, epics, cancellationToken);
        if (basket.Strategy != SyntheticStrategyKind.ManualFormula ||
            !SyntheticRealtimeBarBuilder.UsesNativeOhlc(timeframe))
        {
            return;
        }
        await client.SubscribeOhlcAsync(
            session,
            epics,
            SyntheticRealtimeBarBuilder.StreamingResolution(timeframe),
            cancellationToken);
    }
}
