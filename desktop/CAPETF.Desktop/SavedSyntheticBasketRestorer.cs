namespace CAPETF.Desktop;

public sealed record SavedSyntheticBasketRestoreResult(
    SyntheticBasket Basket,
    SyntheticStrategyKind Strategy);

public static class SavedSyntheticBasketRestorer
{
    public static SavedSyntheticBasketRestoreResult? Restore(
        SavedSyntheticBasket saved,
        IReadOnlyList<MarketInstrument> availableInstruments,
        HistoryLoadResult history,
        string timeframe,
        int periodsPerYear,
        int minimumCandles)
    {
        var savedEpics = saved.Components.Select(component => component.Epic).ToList();
        var validComponentCount = saved.Strategy == SyntheticStrategyKind.ManualFormula
            ? savedEpics.Count is >= 2 and <= 4
            : savedEpics.Count >= 3;
        if (!validComponentCount ||
            savedEpics.Any(string.IsNullOrWhiteSpace) ||
            savedEpics.Distinct(StringComparer.OrdinalIgnoreCase).Count() != savedEpics.Count)
        {
            return null;
        }

        var availableByEpic = availableInstruments
            .Where(instrument => !string.IsNullOrWhiteSpace(instrument.Epic))
            .GroupBy(instrument => instrument.Epic, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        if (savedEpics.Any(epic => !availableByEpic.TryGetValue(epic, out var matches) || matches.Count != 1))
        {
            return null;
        }

        var orderedInstruments = savedEpics.Select(epic => availableByEpic[epic][0]).ToList();
        var basket = saved.Strategy == SyntheticStrategyKind.ManualFormula
            ? ManualSyntheticBasketFactory.Restore(saved, orderedInstruments, history, timeframe, minimumCandles)
            : SyntheticBasketBuilder.BuildSavedFormula(
                saved,
                orderedInstruments,
                history,
                timeframe,
                periodsPerYear,
                minimumCandles);
        if (basket is null) return null;
        basket.Strategy = saved.Strategy;
        return new SavedSyntheticBasketRestoreResult(basket, saved.Strategy);
    }
}
