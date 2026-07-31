namespace CAPETF.Desktop;

public static class SyntheticExecutionBasketSnapshot
{
    public static SavedSyntheticBasket Create(
        SyntheticExecutionRecord execution,
        IReadOnlyList<MarketInstrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if (execution.Legs.Count is < 3 or > 4)
        {
            throw new InvalidOperationException("The execution must contain three or four legs.");
        }

        var instrumentsByEpic = instruments
            .Where(instrument => !string.IsNullOrWhiteSpace(instrument.Epic))
            .GroupBy(instrument => instrument.Epic, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var missing = execution.Legs
            .Select(leg => leg.Epic)
            .Where(epic => !instrumentsByEpic.ContainsKey(epic))
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Execution symbols are missing from the universe: {string.Join(", ", missing)}.");
        }

        var ordered = execution.Legs.Select(leg => instrumentsByEpic[leg.Epic]).ToArray();
        var region = CommonValue(ordered.Select(instrument => instrument.Region), "Other");
        var currency = CommonValue(ordered.Select(instrument => instrument.Currency), execution.MarginCurrency);
        var symbol = execution.BasketId.Split('|', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? $"SYN-{execution.ExecutionId[..Math.Min(8, execution.ExecutionId.Length)].ToUpperInvariant()}";
        var weight = 100m / execution.Legs.Count;
        return new SavedSyntheticBasket(
            $"execution-{execution.ExecutionId}",
            $"Open {symbol}",
            symbol,
            $"{region} / {currency} / All",
            SyntheticStrategyKind.SimilarToSelectedSymbol,
            execution.CreatedUtc,
            execution.UpdatedUtc,
            execution.Legs.Select((leg, index) => new SavedSyntheticComponent(
                leg.Epic,
                ordered[index].Name,
                ordered[index].Currency,
                weight,
                leg.Multiplier,
                leg.ReferencePrice)).ToArray());
    }

    private static string CommonValue(IEnumerable<string> values, string fallback)
    {
        var distinct = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return distinct.Length == 1 ? distinct[0] : fallback;
    }
}
