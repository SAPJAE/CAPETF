using System.Globalization;

namespace CAPETF.Desktop;

public sealed record ManualSyntheticTerm(decimal Multiplier, string InstrumentToken);

public sealed class ManualSyntheticFormula
{
    public const string CryptoPreset = "9 ETHUSD + 0.2 BTCUSD";

    private ManualSyntheticFormula(IReadOnlyList<ManualSyntheticTerm> terms)
    {
        Terms = terms;
    }

    public IReadOnlyList<ManualSyntheticTerm> Terms { get; }

    public bool IsCryptoPreset =>
        Terms.Count == 2 &&
        Terms[0].Multiplier == 9m &&
        Terms[1].Multiplier == 0.2m &&
        string.Equals(Terms[0].InstrumentToken, "ETHUSD", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Terms[1].InstrumentToken, "BTCUSD", StringComparison.OrdinalIgnoreCase);

    public static ManualSyntheticFormula Parse(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new FormatException("Manual formula is required.");
        }

        var rawTerms = source.Split('+');
        if (rawTerms.Length is < 2 or > 4)
        {
            throw new FormatException("Manual formula must contain two to four terms.");
        }

        var terms = new List<ManualSyntheticTerm>(rawTerms.Length);
        foreach (var rawTerm in rawTerms)
        {
            var term = rawTerm.Trim();
            var separator = term.IndexOfAny([' ', '\t']);
            if (separator <= 0)
            {
                throw new FormatException($"Manual formula term '{term}' has an invalid decimal multiplier.");
            }

            var multiplierText = term[..separator].Trim();
            var instrumentToken = term[(separator + 1)..].Trim();
            if (multiplierText.StartsWith("-", StringComparison.Ordinal) &&
                IsStrictUnsignedDecimal(multiplierText[1..]))
            {
                throw new FormatException("Manual formula multipliers must be greater than zero.");
            }
            if (!IsStrictUnsignedDecimal(multiplierText) ||
                !decimal.TryParse(
                    multiplierText,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var multiplier))
            {
                throw new FormatException($"Manual formula multiplier '{multiplierText}' is invalid.");
            }
            if (multiplier <= 0)
            {
                throw new FormatException("Manual formula multipliers must be greater than zero.");
            }
            if (string.IsNullOrWhiteSpace(instrumentToken))
            {
                throw new FormatException("Every manual formula multiplier must be followed by an instrument.");
            }

            terms.Add(new ManualSyntheticTerm(multiplier, instrumentToken));
        }

        return new ManualSyntheticFormula(terms);
    }

    private static bool IsStrictUnsignedDecimal(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] == '.' || value[^1] == '.') return false;
        var decimalPoints = 0;
        foreach (var character in value)
        {
            if (character == '.')
            {
                decimalPoints++;
                if (decimalPoints > 1) return false;
            }
            else if (character is < '0' or > '9')
            {
                return false;
            }
        }
        return true;
    }

    public static string Format(IEnumerable<SavedSyntheticComponent> components) =>
        string.Join(" + ", components.Select(component =>
            $"{component.FormulaMultiplier.ToString("G29", CultureInfo.InvariantCulture)} {component.Epic}"));
}

public static class ManualSyntheticBasketFactory
{
    public static IReadOnlyList<MarketInstrument> Resolve(
        string block,
        ManualSyntheticFormula formula,
        IReadOnlyList<MarketInstrument> instruments) =>
        ResolveTerms(block, formula, instruments).Select(term => term.Instrument).ToList();

    public static SyntheticBasket Create(
        string symbol,
        string block,
        ManualSyntheticFormula formula,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles)
    {
        var resolved = ResolveTerms(block, formula, instruments);
        return Build(symbol, block, resolved, candles, timeframe: null, minimumCandles: 1, savedComponents: null);
    }

    public static SyntheticBasket Create(
        string symbol,
        string block,
        ManualSyntheticFormula formula,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        string timeframe,
        int minimumCandles)
    {
        var resolved = ResolveTerms(block, formula, instruments);
        return Build(symbol, block, resolved, candles, timeframe, minimumCandles, savedComponents: null);
    }

    internal static SyntheticBasket? Restore(
        SavedSyntheticBasket saved,
        IReadOnlyList<MarketInstrument> orderedInstruments,
        HistoryLoadResult history,
        string timeframe,
        int minimumCandles)
    {
        if (saved.Components.Count is < 2 or > 4 || orderedInstruments.Count != saved.Components.Count)
        {
            return null;
        }

        var resolved = new List<ResolvedTerm>(orderedInstruments.Count);
        for (var index = 0; index < orderedInstruments.Count; index++)
        {
            var savedComponent = saved.Components[index];
            var instrument = orderedInstruments[index];
            if (!string.Equals(savedComponent.Epic, instrument.Epic, StringComparison.OrdinalIgnoreCase) ||
                savedComponent.FormulaMultiplier <= 0)
            {
                return null;
            }
            resolved.Add(new ResolvedTerm(savedComponent.FormulaMultiplier, instrument));
        }

        try
        {
            ValidateResolvedBlock(saved.Block, resolved);
            return Build(
                saved.Symbol,
                saved.Block,
                resolved,
                history.CandlesByEpic,
                timeframe,
                minimumCandles,
                saved.Components);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static IReadOnlyList<ResolvedTerm> ResolveTerms(
        string block,
        ManualSyntheticFormula formula,
        IReadOnlyList<MarketInstrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(formula);
        ArgumentNullException.ThrowIfNull(instruments);
        var blockCurrency = CryptoBlockCurrency(block);
        var blockInstruments = instruments.Where(instrument =>
                CapitalInstrumentTypes.IsCrypto(instrument) &&
                string.Equals(instrument.Group, block, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var resolved = new List<ResolvedTerm>(formula.Terms.Count);

        foreach (var term in formula.Terms)
        {
            var exactIdentifierMatches = blockInstruments.Where(instrument =>
                    string.Equals(instrument.Epic?.Trim(), term.InstrumentToken, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(instrument.Symbol?.Trim(), term.InstrumentToken, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var instrument = ResolveTier(exactIdentifierMatches, term.InstrumentToken, block);

            if (instrument is null)
            {
                var exactNameMatches = blockInstruments.Where(candidate =>
                        string.Equals(candidate.Name?.Trim(), term.InstrumentToken, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                instrument = ResolveTier(exactNameMatches, term.InstrumentToken, block);
            }

            var normalizedToken = NormalizeIdentifier(term.InstrumentToken);
            if (instrument is null && normalizedToken.Length >= 4)
            {
                var normalizedMatches = blockInstruments.Where(candidate =>
                        string.Equals(NormalizeIdentifier(candidate.Epic), normalizedToken, StringComparison.Ordinal) ||
                        string.Equals(NormalizeIdentifier(candidate.Symbol), normalizedToken, StringComparison.Ordinal))
                    .ToList();
                instrument = ResolveTier(normalizedMatches, term.InstrumentToken, block);
            }

            if (instrument is null)
            {
                var outsideMatches = instruments.Where(candidate =>
                        string.Equals(candidate.Epic?.Trim(), term.InstrumentToken, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(candidate.Symbol?.Trim(), term.InstrumentToken, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(candidate.Name?.Trim(), term.InstrumentToken, StringComparison.OrdinalIgnoreCase) ||
                        normalizedToken.Length >= 4 &&
                        (string.Equals(NormalizeIdentifier(candidate.Epic), normalizedToken, StringComparison.Ordinal) ||
                         string.Equals(NormalizeIdentifier(candidate.Symbol), normalizedToken, StringComparison.Ordinal)))
                    .ToList();
                if (outsideMatches.Count == 0)
                {
                    throw new InvalidOperationException($"Manual formula instrument '{term.InstrumentToken}' was not found.");
                }
                if (outsideMatches.Any(candidate => !CapitalInstrumentTypes.IsCrypto(candidate)))
                {
                    throw new InvalidOperationException(
                        $"Manual formula instrument '{term.InstrumentToken}' is not a Crypto instrument in selected block '{block}'.");
                }
                if (outsideMatches.Any(candidate =>
                        !string.Equals(candidate.Currency?.Trim(), blockCurrency, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        $"Manual formula instrument '{term.InstrumentToken}' has a currency outside the selected {blockCurrency} block.");
                }
                throw new InvalidOperationException(
                    $"Manual formula instrument '{term.InstrumentToken}' is not available in selected block '{block}'.");
            }

            if (resolved.Any(item => string.Equals(item.Instrument.Epic, instrument.Epic, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Manual formula contains duplicate instrument '{instrument.Epic}'.");
            }
            resolved.Add(new ResolvedTerm(term.Multiplier, instrument));
        }

        ValidateResolvedBlock(block, resolved);
        return resolved;
    }

    private static MarketInstrument? ResolveTier(
        IReadOnlyList<MarketInstrument> matches,
        string instrumentToken,
        string block)
    {
        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Manual formula instrument '{instrumentToken}' is ambiguous in selected block '{block}'.");
        }
        return matches.Count == 1 ? matches[0] : null;
    }

    private static SyntheticBasket Build(
        string symbol,
        string block,
        IReadOnlyList<ResolvedTerm> resolved,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        string? timeframe,
        int minimumCandles,
        IReadOnlyList<SavedSyntheticComponent>? savedComponents)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Synthetic symbol is required.", nameof(symbol));
        if (minimumCandles < 1) throw new ArgumentOutOfRangeException(nameof(minimumCandles));
        ValidateResolvedBlock(block, resolved);

        var rowsByKey = new List<Dictionary<string, OhlcPoint>>(resolved.Count);
        foreach (var term in resolved)
        {
            if (!candles.TryGetValue(term.Instrument.Epic, out var rows))
            {
                throw new InvalidOperationException($"No candle history is available for '{term.Instrument.Epic}'.");
            }

            var keyed = rows
                .OrderBy(row => row.Time)
                .GroupBy(row => AlignmentKey(row.Time, timeframe), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
            if (keyed.Count < minimumCandles)
            {
                throw new InvalidOperationException($"Not enough candle history is available for '{term.Instrument.Epic}'.");
            }
            rowsByKey.Add(keyed);
        }

        var sharedKeys = rowsByKey[0].Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var rows in rowsByKey.Skip(1)) sharedKeys.IntersectWith(rows.Keys);
        var orderedKeys = sharedKeys.OrderBy(key => rowsByKey[0][key].Time).ToList();
        if (orderedKeys.Count < minimumCandles)
        {
            throw new InvalidOperationException("The manual formula legs do not have enough shared candle timestamps.");
        }

        var syntheticCandles = orderedKeys.Select(key => BuildCandle(
            rowsByKey[0][key].Time,
            resolved.Select((term, index) => (term.Multiplier, rowsByKey[index][key])).ToList())).ToList();
        var finalKey = orderedKeys[^1];
        var referencePrices = resolved.Select((_, index) => rowsByKey[index][finalKey].Close).ToList();
        var weights = savedComponents is null
            ? ContributionWeights(resolved, referencePrices)
            : savedComponents.Select(component => component.Weight).ToList();

        var basket = new SyntheticBasket
        {
            Symbol = symbol.Trim(),
            Block = block.Trim(),
            Strategy = SyntheticStrategyKind.ManualFormula,
            BasketPrice = syntheticCandles[^1].Close,
            LastUpdated = syntheticCandles[^1].Time,
        };
        for (var index = 0; index < resolved.Count; index++)
        {
            var savedComponent = savedComponents?[index];
            var referencePrice = savedComponent?.ReferencePrice ?? referencePrices[index];
            basket.Components.Add(new SyntheticComponent(
                resolved[index].Instrument,
                weights[index],
                annualizedVolatilityPct: 0m,
                fourYearReturnPct: 0m)
            {
                FormulaMultiplier = resolved[index].Multiplier,
                FormulaReferencePrice = referencePrice,
                SyntheticBaselinePrice = referencePrice,
                LastAppliedPrice = referencePrices[index],
            });
        }
        foreach (var candle in syntheticCandles) basket.Candles.Add(candle);
        SyntheticQuoteCalculator.Refresh(basket);
        return basket;
    }

    private static OhlcPoint BuildCandle(
        DateTimeOffset time,
        IReadOnlyList<(decimal Multiplier, OhlcPoint Candle)> legs)
    {
        decimal open = 0m, high = 0m, low = 0m, close = 0m;
        foreach (var (multiplier, candle) in legs)
        {
            open += multiplier * candle.Open;
            close += multiplier * candle.Close;
            high += multiplier >= 0 ? multiplier * candle.High : multiplier * candle.Low;
            low += multiplier >= 0 ? multiplier * candle.Low : multiplier * candle.High;
        }

        return new OhlcPoint(
            time,
            decimal.Round(open, 6),
            decimal.Round(high, 6),
            decimal.Round(low, 6),
            decimal.Round(close, 6));
    }

    private static IReadOnlyList<decimal> ContributionWeights(
        IReadOnlyList<ResolvedTerm> resolved,
        IReadOnlyList<decimal> referencePrices)
    {
        var contributions = resolved.Select((term, index) => Math.Abs(term.Multiplier * referencePrices[index])).ToList();
        var total = contributions.Sum();
        if (total <= 0) return Enumerable.Repeat(0m, resolved.Count).ToList();

        var weights = contributions.Select(contribution => decimal.Round(contribution / total * 100m, 4)).ToList();
        weights[^1] = decimal.Round(100m - weights.Take(weights.Count - 1).Sum(), 4);
        return weights;
    }

    private static void ValidateResolvedBlock(string block, IReadOnlyList<ResolvedTerm> resolved)
    {
        var currency = CryptoBlockCurrency(block);
        if (resolved.Count is < 2 or > 4)
        {
            throw new InvalidOperationException("Manual formula must resolve to two to four instruments.");
        }
        if (resolved.Select(term => term.Instrument.Epic).Distinct(StringComparer.OrdinalIgnoreCase).Count() != resolved.Count)
        {
            throw new InvalidOperationException("Manual formula contains duplicate instruments.");
        }
        if (resolved.Any(term =>
                !CapitalInstrumentTypes.IsCrypto(term.Instrument) ||
                !string.Equals(term.Instrument.Currency?.Trim(), currency, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(term.Instrument.Group, block, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Manual formula legs must all use currency {currency} in selected block '{block}'.");
        }
    }

    private static string CryptoBlockCurrency(string block)
    {
        var parts = (block ?? "").Split('/').Select(part => part.Trim()).ToArray();
        if (parts.Length != 3 ||
            !string.Equals(parts[0], "Crypto", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new InvalidOperationException("Manual formulas require a selected Crypto currency block.");
        }
        return parts[1];
    }

    private static string AlignmentKey(DateTimeOffset time, string? timeframe) =>
        string.IsNullOrWhiteSpace(timeframe)
            ? $"T:{time.ToUniversalTime().Ticks}"
            : SyntheticHistoryService.AlignmentKey(time, timeframe);

    private static string NormalizeIdentifier(string? value) =>
        new((value ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private sealed record ResolvedTerm(decimal Multiplier, MarketInstrument Instrument);
}
