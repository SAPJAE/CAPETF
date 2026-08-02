namespace CAPETF.Desktop;

internal sealed class CapitalApiSyntheticMarginDataSource(CapitalApiClient api) : ISyntheticMarginDataSource
{
    public Task<CapitalAccountSnapshot> GetActiveAccountAsync(CancellationToken cancellationToken) =>
        api.GetActiveAccountAsync(cancellationToken);

    public Task<MarketInstrument?> GetMarketDetailsAsync(string epic, CancellationToken cancellationToken) =>
        api.GetMarketDetailsAsync(epic, cancellationToken);

    public Task<IReadOnlyList<MarketInstrument>> SearchMarketsAsync(string query, CancellationToken cancellationToken) =>
        api.SearchMarketsAsync(query, cancellationToken);
}

internal static class SyntheticMarginPreviewPublication
{
    public static bool IsCurrent(
        CancellationToken requestToken,
        CancellationTokenSource request,
        CancellationTokenSource? currentRequest,
        SyntheticBasket requestBasket,
        SyntheticBasket? currentBasket) =>
        !requestToken.IsCancellationRequested &&
        ReferenceEquals(currentRequest, request) &&
        ReferenceEquals(currentBasket, requestBasket);
}

internal static class SyntheticMarginPreviewInput
{
    public const string InvalidReason = "Enter a positive whole number of synthetic lots.";

    public static bool TryValidate(decimal? value, out decimal syntheticLots, out string error)
    {
        if (value is > 0 && value.Value == decimal.Truncate(value.Value))
        {
            syntheticLots = value.Value;
            error = "";
            return true;
        }

        syntheticLots = 0m;
        error = InvalidReason;
        return false;
    }
}

internal sealed class SyntheticMarginPreviewService
{
    private static readonly TimeSpan AccountCacheDuration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ConversionCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ConversionFailureCacheDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MetadataAttemptCacheDuration = TimeSpan.FromSeconds(30);

    private readonly ISyntheticMarginDataSource _source;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly object _cacheLock = new();
    private readonly Dictionary<string, CachedConversion> _conversionCache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CachedMetadataAttempt> _metadataAttempts =
        new(StringComparer.OrdinalIgnoreCase);
    private CapitalAccountSnapshot? _cachedAccount;
    private DateTimeOffset _accountCachedAt;

    public SyntheticMarginPreviewService(
        ISyntheticMarginDataSource source,
        Func<DateTimeOffset>? utcNow = null)
    {
        _source = source;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public void InvalidateCaches()
    {
        lock (_cacheLock)
        {
            _cachedAccount = null;
            _accountCachedAt = default;
            _conversionCache.Clear();
            _metadataAttempts.Clear();
        }
    }

    public async Task<SyntheticMarginSummary> BuildAsync(
        SyntheticBasket basket,
        decimal basketNotional,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(basket);
        cancellationToken.ThrowIfCancellationRequested();

        await RefreshMissingMarginMetadataAsync(basket, cancellationToken);
        var accountResult = await GetAccountAsync(cancellationToken);
        var account = accountResult.Account;
        if (string.IsNullOrWhiteSpace(account.Currency))
        {
            throw new InvalidOperationException("Capital.com active account currency is unavailable.");
        }

        var basketCurrency = BasketCurrency(basket);
        var conversionRate = await GetConversionRateAsync(
            basketCurrency,
            account.Currency.Trim(),
            cancellationToken);
        var buy = SyntheticMarginCalculator.CalculateSyntheticLotsSide(
            basket,
            "BUY",
            basketNotional,
            account.Currency,
            conversionRate);
        var sell = SyntheticMarginCalculator.CalculateSyntheticLotsSide(
            basket,
            "SELL",
            basketNotional,
            account.Currency,
            conversionRate);
        return SyntheticMarginCalculator.Combine(
            account,
            buy,
            sell,
            accountResult.IsStale,
            accountResult.Error);
    }

    private async Task RefreshMissingMarginMetadataAsync(
        SyntheticBasket basket,
        CancellationToken cancellationToken)
    {
        foreach (var component in basket.Components)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var instrument = component.Instrument;
            if (!NeedsMarginMetadata(instrument)) continue;

            var details = await GetMetadataAttempt(instrument.Epic).WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (details is null) continue;

            if (string.IsNullOrWhiteSpace(instrument.Currency) &&
                !string.IsNullOrWhiteSpace(details.Currency))
            {
                instrument.Currency = details.Currency.Trim();
            }
            if (instrument.LotSize is not > 0 && details.LotSize is > 0) instrument.LotSize = details.LotSize;
            if (instrument.MinDealSize is not > 0 && details.MinDealSize is > 0) instrument.MinDealSize = details.MinDealSize;
            if (instrument.MinSizeIncrement is not > 0 && details.MinSizeIncrement is > 0)
            {
                instrument.MinSizeIncrement = details.MinSizeIncrement;
            }
            if (instrument.MarginFactor is null && details.MarginFactor is not null)
            {
                instrument.MarginFactor = details.MarginFactor;
            }
            if (string.IsNullOrWhiteSpace(instrument.MarginFactorUnit) &&
                !string.IsNullOrWhiteSpace(details.MarginFactorUnit))
            {
                instrument.MarginFactorUnit = details.MarginFactorUnit;
            }
        }
    }

    private Task<MarketInstrument?> GetMetadataAttempt(string epic)
    {
        lock (_cacheLock)
        {
            var now = _utcNow();
            if (_metadataAttempts.TryGetValue(epic, out var cached))
            {
                if (!cached.Details.IsCompleted) return cached.Details;
                cached.CompletedAt ??= now;
                if (now - cached.CompletedAt.Value < MetadataAttemptCacheDuration)
                {
                    return cached.Details;
                }
            }

            Task<MarketInstrument?> details;
            try
            {
                details = _source.GetMarketDetailsAsync(epic, CancellationToken.None);
            }
            catch (Exception ex)
            {
                details = Task.FromException<MarketInstrument?>(ex);
            }
            var attempt = new CachedMetadataAttempt(details);
            _metadataAttempts[epic] = attempt;
            _ = details.ContinueWith(
                _ =>
                {
                    lock (_cacheLock)
                    {
                        attempt.CompletedAt ??= _utcNow();
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return details;
        }
    }

    private async Task<AccountLookupResult> GetAccountAsync(CancellationToken cancellationToken)
    {
        var now = _utcNow();
        CapitalAccountSnapshot? staleAccount;
        lock (_cacheLock)
        {
            if (_cachedAccount is not null && now - _accountCachedAt < AccountCacheDuration)
            {
                return new AccountLookupResult(_cachedAccount, false, "");
            }
            staleAccount = _cachedAccount;
        }

        try
        {
            var account = await _source.GetActiveAccountAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            lock (_cacheLock)
            {
                _cachedAccount = account;
                _accountCachedAt = _utcNow();
            }
            return new AccountLookupResult(account, false, "");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (staleAccount is not null)
        {
            return new AccountLookupResult(staleAccount, true, ex.Message);
        }
    }

    private async Task<decimal> GetConversionRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken)
    {
        var fromIdentity = ConversionCurrencyIdentity(fromCurrency);
        var toIdentity = ConversionCurrencyIdentity(toCurrency);
        if (string.Equals(fromIdentity, toIdentity, StringComparison.OrdinalIgnoreCase)) return 1m;

        var cacheKey = $"{fromIdentity}/{toIdentity}";
        var now = _utcNow();
        lock (_cacheLock)
        {
            if (_conversionCache.TryGetValue(cacheKey, out var cached))
            {
                var duration = cached.Rate is > 0
                    ? ConversionCacheDuration
                    : ConversionFailureCacheDuration;
                if (now - cached.RetrievedAt < duration)
                {
                    if (cached.Rate is > 0) return cached.Rate.Value;
                    throw ConversionUnavailable(fromIdentity, toIdentity);
                }
            }
        }

        var direct = await FindMidpointAsync(fromIdentity, toIdentity, cancellationToken);
        var rate = direct;
        if (rate is null)
        {
            var inverse = await FindMidpointAsync(toIdentity, fromIdentity, cancellationToken);
            if (inverse is > 0) rate = 1m / inverse.Value;
        }

        if (rate is not > 0)
        {
            lock (_cacheLock)
            {
                _conversionCache[cacheKey] = new CachedConversion(null, _utcNow());
            }
            throw ConversionUnavailable(fromIdentity, toIdentity);
        }

        lock (_cacheLock)
        {
            _conversionCache[cacheKey] = new CachedConversion(rate.Value, _utcNow());
        }
        return rate.Value;
    }

    private async Task<decimal?> FindMidpointAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken)
    {
        var markets = await _source.SearchMarketsAsync($"{fromCurrency}/{toCurrency}", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var market in markets.Where(item => IsOrderedCurrencyPair(item, fromCurrency, toCurrency)))
        {
            if (string.IsNullOrWhiteSpace(market.Epic)) continue;
            var details = await _source.GetMarketDetailsAsync(market.Epic, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (details?.Bid is > 0 && details.Offer is > 0)
            {
                return (details.Bid.Value + details.Offer.Value) / 2m;
            }
        }

        return null;
    }

    private static bool NeedsMarginMetadata(MarketInstrument instrument) =>
        string.IsNullOrWhiteSpace(instrument.Currency) ||
        instrument.LotSize is not > 0 ||
        instrument.MinDealSize is not > 0 ||
        instrument.MinSizeIncrement is not > 0 ||
        instrument.MarginFactor is null ||
        string.IsNullOrWhiteSpace(instrument.MarginFactorUnit);

    private static string BasketCurrency(SyntheticBasket basket)
    {
        var currencies = basket.Components
            .Select(component => component.Instrument.Currency.Trim())
            .Where(currency => !string.IsNullOrWhiteSpace(currency))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (currencies.Count != 1 || basket.Components.Any(component =>
                string.IsNullOrWhiteSpace(component.Instrument.Currency)))
        {
            throw new InvalidOperationException("Synthetic basket currency is unavailable or inconsistent.");
        }

        return currencies[0];
    }

    private static bool IsOrderedCurrencyPair(MarketInstrument instrument, string fromCurrency, string toCurrency)
    {
        if (!instrument.Type.Contains("CURRENC", StringComparison.OrdinalIgnoreCase)) return false;
        var orderedPair = NormalizePairIdentity(fromCurrency + toCurrency);
        var symbol = NormalizePairIdentity(instrument.Symbol);
        if (symbol.Contains(orderedPair, StringComparison.OrdinalIgnoreCase)) return true;
        var name = NormalizePairIdentity(instrument.Name);
        var fromIndex = name.IndexOf(NormalizePairIdentity(fromCurrency), StringComparison.OrdinalIgnoreCase);
        var toIndex = name.IndexOf(NormalizePairIdentity(toCurrency), StringComparison.OrdinalIgnoreCase);
        return fromIndex >= 0 && toIndex > fromIndex;
    }

    private static string NormalizePairIdentity(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string ConversionCurrencyIdentity(string currency)
    {
        var identity = currency.Trim();
        if (identity.Length == 4 &&
            char.ToUpperInvariant(identity[3]) == 'D' &&
            identity[..3].All(char.IsLetter))
        {
            identity = identity[..3];
        }

        return identity.ToUpperInvariant();
    }

    private static InvalidOperationException ConversionUnavailable(string fromCurrency, string toCurrency) =>
        new($"Margin conversion {fromCurrency}/{toCurrency} is unavailable from Capital.com.");

    private sealed record CachedConversion(decimal? Rate, DateTimeOffset RetrievedAt);
    private sealed record AccountLookupResult(CapitalAccountSnapshot Account, bool IsStale, string Error);
    private sealed class CachedMetadataAttempt(Task<MarketInstrument?> details)
    {
        public Task<MarketInstrument?> Details { get; } = details;
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
