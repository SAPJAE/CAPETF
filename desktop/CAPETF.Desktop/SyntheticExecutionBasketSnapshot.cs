namespace CAPETF.Desktop;

public static class SyntheticExecutionBasketSnapshot
{
    public static SavedSyntheticBasket Create(
        SyntheticExecutionRecord execution,
        IReadOnlyList<MarketInstrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(execution);
        var isManual = execution.BasketQuantity is > 0m;
        var validLegCount = isManual
            ? execution.Legs.Count is >= 2 and <= 4
            : execution.Legs.Count is >= 3 and <= 4;
        if (!validLegCount)
        {
            throw new InvalidOperationException(isManual
                ? "The manual execution must contain two to four legs."
                : "The execution must contain three or four legs.");
        }
        if (isManual)
        {
            foreach (var leg in execution.Legs)
            {
                var exactQuantity = Math.Abs(leg.Multiplier * execution.BasketQuantity!.Value);
                if (leg.Quantity != exactQuantity)
                {
                    throw new InvalidOperationException(
                        $"Execution leg {leg.Epic} does not preserve basket quantity {execution.BasketQuantity.Value} and multiplier {leg.Multiplier}.");
                }
            }
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
        var strategy = isManual
            ? SyntheticStrategyKind.ManualFormula
            : SyntheticStrategyKind.SimilarToSelectedSymbol;
        var totalNotional = execution.Legs.Sum(leg => leg.Notional);
        return new SavedSyntheticBasket(
            $"execution-{execution.ExecutionId}",
            $"Open {symbol}",
            symbol,
            $"{region} / {currency} / All",
            strategy,
            execution.CreatedUtc,
            execution.UpdatedUtc,
            execution.Legs.Select((leg, index) => new SavedSyntheticComponent(
                leg.Epic,
                ordered[index].Name,
                ordered[index].Currency,
                isManual && totalNotional > 0m ? leg.Notional / totalNotional * 100m : 100m / execution.Legs.Count,
                leg.Multiplier,
                leg.ReferencePrice)).ToArray(),
            execution.BasketQuantity);
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
