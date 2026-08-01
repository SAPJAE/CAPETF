namespace CAPETF.Desktop;

public sealed class ActiveSyntheticBasketState
{
    public SyntheticBasket? Basket { get; private set; }

    public SyntheticStrategyKind? Strategy => Basket?.Strategy;

    public void Activate(SyntheticBasket basket, SyntheticStrategyKind strategy)
    {
        ArgumentNullException.ThrowIfNull(basket);
        basket.Strategy = strategy;
        Basket = basket;
    }

    public void Clear() => Basket = null;

    public string SuggestedSavedBasketName()
    {
        var basket = Basket ?? throw new InvalidOperationException("Build a synthetic basket before saving.");
        var suffix = basket.Strategy == SyntheticStrategyKind.SimilarToSelectedSymbol
            ? "SIMILAR"
            : basket.Strategy.ToString().ToUpperInvariant();
        return $"{basket.Symbol}-{suffix}";
    }

    public SavedSyntheticBasket CreateSavedBasket(string name, TerminalUniverseKind? universeKind = null)
    {
        var basket = Basket ?? throw new InvalidOperationException("Build a synthetic basket before saving.");
        return SavedSyntheticBasket.FromBasket(name, basket.Strategy, basket, universeKind);
    }

    public SyntheticBasket? RebuildHistory(
        HistoryLoadResult history,
        string timeframe,
        int periodsPerYear,
        int minimumCandles)
    {
        var basket = Basket ?? throw new InvalidOperationException("Build a synthetic basket before reloading history.");
        var selectedComponents = basket.Components.Select(component => component.Instrument).ToList();
        if (basket.Strategy == SyntheticStrategyKind.ManualFormula)
        {
            var saved = SavedSyntheticBasket.FromBasket(basket.Symbol, basket.Strategy, basket);
            return ManualSyntheticBasketFactory.Restore(saved, selectedComponents, history, timeframe, minimumCandles);
        }

        var rebuilt = SyntheticHistoryService.BuildSelected(
            basket.Block,
            selectedComponents,
            history,
            timeframe,
            periodsPerYear,
            minimumCandles);
        if (rebuilt is not null) rebuilt.Strategy = basket.Strategy;
        return rebuilt;
    }
}
