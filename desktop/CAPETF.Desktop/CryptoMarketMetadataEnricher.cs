namespace CAPETF.Desktop;

public sealed class CryptoMarketMetadataEnricher
{
    public const int DefaultMaximumConcurrency = 4;

    private readonly Func<string, CancellationToken, Task<MarketInstrument?>> _loadDetails;
    private readonly int _maximumConcurrency;
    private readonly Dictionary<string, MarketInstrument> _detailsByEpic =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cacheGate = new();

    public CryptoMarketMetadataEnricher(
        Func<string, CancellationToken, Task<MarketInstrument?>> loadDetails,
        int maximumConcurrency = DefaultMaximumConcurrency)
    {
        _loadDetails = loadDetails ?? throw new ArgumentNullException(nameof(loadDetails));
        if (maximumConcurrency < 1) throw new ArgumentOutOfRangeException(nameof(maximumConcurrency));
        _maximumConcurrency = maximumConcurrency;
    }

    public async Task<IReadOnlyList<MarketInstrument>> EnrichAsync(
        IReadOnlyList<MarketInstrument> summaries,
        Action<int, int>? reportProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        cancellationToken.ThrowIfCancellationRequested();

        var candidates = summaries
            .Where(NeedsEnrichment)
            .GroupBy(item => item.Epic.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var completed = 0;
        reportProgress?.Invoke(completed, candidates.Count);

        var resolved = new Dictionary<string, MarketInstrument>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<MarketInstrument>();
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryGetCached(candidate.Epic, out var details))
            {
                resolved[candidate.Epic] = details;
                reportProgress?.Invoke(++completed, candidates.Count);
            }
            else
            {
                missing.Add(candidate);
            }
        }

        await Parallel.ForEachAsync(
            missing,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _maximumConcurrency,
                CancellationToken = cancellationToken,
            },
            async (candidate, token) =>
            {
                try
                {
                    var details = await _loadDetails(candidate.Epic, token);
                    if (details is not null)
                    {
                        Cache(candidate.Epic, details);
                        lock (resolved)
                        {
                            resolved[candidate.Epic] = details;
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // A single unavailable market must not prevent the Crypto universe from loading.
                }
                finally
                {
                    reportProgress?.Invoke(Interlocked.Increment(ref completed), candidates.Count);
                }
            });

        return summaries
            .Select(summary => resolved.TryGetValue(summary.Epic, out var details)
                ? Merge(summary, details)
                : summary)
            .ToList();
    }

    public static bool NeedsEnrichment(MarketInstrument instrument) =>
        CapitalInstrumentTypes.IsCrypto(instrument) &&
        !string.IsNullOrWhiteSpace(instrument.Epic) &&
        string.IsNullOrWhiteSpace(instrument.Currency);

    private bool TryGetCached(string epic, out MarketInstrument details)
    {
        lock (_cacheGate)
        {
            return _detailsByEpic.TryGetValue(epic, out details!);
        }
    }

    private void Cache(string epic, MarketInstrument details)
    {
        lock (_cacheGate)
        {
            _detailsByEpic[epic] = details;
        }
    }

    private static MarketInstrument Merge(MarketInstrument summary, MarketInstrument details)
    {
        var merged = new MarketInstrument
        {
            Epic = First(summary.Epic, details.Epic),
            Name = First(details.Name, summary.Name),
            Symbol = First(details.Symbol, summary.Symbol),
            Type = First(details.Type, summary.Type),
            Currency = First(details.Currency, summary.Currency),
            Country = First(details.Country, summary.Country),
            Region = First(details.Region, summary.Region),
            Sector = First(details.Sector, summary.Sector),
            Status = First(details.Status, summary.Status),
            LotSize = details.LotSize ?? summary.LotSize,
            MinDealSize = details.MinDealSize ?? summary.MinDealSize,
            MinSizeIncrement = details.MinSizeIncrement ?? summary.MinSizeIncrement,
            MarginFactor = details.MarginFactor ?? summary.MarginFactor,
            MarginFactorUnit = First(details.MarginFactorUnit, summary.MarginFactorUnit),
            Price = summary.Price ?? details.Price,
            Bid = summary.Bid ?? details.Bid,
            Offer = summary.Offer ?? details.Offer,
            IntradayReturn = summary.IntradayReturn ?? details.IntradayReturn,
            ChangePercent = summary.ChangePercent ?? details.ChangePercent,
            Low = summary.Low ?? details.Low,
            High = summary.High ?? details.High,
            Sma20 = summary.Sma20 ?? details.Sma20,
            Sma50 = summary.Sma50 ?? details.Sma50,
            AlertPrice = summary.AlertPrice ?? details.AlertPrice,
            IsWatchlisted = summary.IsWatchlisted || details.IsWatchlisted,
            LastTickAt = summary.LastTickAt ?? details.LastTickAt,
        };
        foreach (var point in summary.Points.Count > 0 ? summary.Points : details.Points) merged.Points.Add(point);
        return merged;
    }

    private static string First(string preferred, string fallback) =>
        !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback;
}
