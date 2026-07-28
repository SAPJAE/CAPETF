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

internal sealed class SyntheticMarginPreviewService
{
    private static readonly TimeSpan AccountCacheDuration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ConversionCacheDuration = TimeSpan.FromSeconds(30);

    private readonly ISyntheticMarginDataSource _source;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<string, CachedConversion> _conversionCache =
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

    public async Task<SyntheticMarginSummary> BuildAsync(
        SyntheticBasket basket,
        decimal basketNotional,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(basket);
        cancellationToken.ThrowIfCancellationRequested();

        await RefreshMissingMarginMetadataAsync(basket, cancellationToken);
        var account = await GetAccountAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(account.Currency))
        {
            throw new InvalidOperationException("Capital.com active account currency is unavailable.");
        }

        var basketCurrency = BasketCurrency(basket);
        var conversionRate = await GetConversionRateAsync(
            basketCurrency,
            account.Currency.Trim(),
            cancellationToken);
        var buy = SyntheticMarginCalculator.CalculateSide(
            basket,
            "BUY",
            basketNotional,
            account.Currency,
            conversionRate);
        var sell = SyntheticMarginCalculator.CalculateSide(
            basket,
            "SELL",
            basketNotional,
            account.Currency,
            conversionRate);
        return SyntheticMarginCalculator.Combine(account, buy, sell);
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

            var details = await _source.GetMarketDetailsAsync(instrument.Epic, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (details is null) continue;

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

    private async Task<CapitalAccountSnapshot> GetAccountAsync(CancellationToken cancellationToken)
    {
        var now = _utcNow();
        if (_cachedAccount is not null && now - _accountCachedAt < AccountCacheDuration)
        {
            return _cachedAccount;
        }

        var account = await _source.GetActiveAccountAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        _cachedAccount = account;
        _accountCachedAt = _utcNow();
        return account;
    }

    private async Task<decimal> GetConversionRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase)) return 1m;

        var cacheKey = $"{fromCurrency}/{toCurrency}";
        var now = _utcNow();
        if (_conversionCache.TryGetValue(cacheKey, out var cached) &&
            now - cached.RetrievedAt < ConversionCacheDuration)
        {
            return cached.Rate;
        }

        var direct = await FindMidpointAsync(fromCurrency, toCurrency, cancellationToken);
        var rate = direct;
        if (rate is null)
        {
            var inverse = await FindMidpointAsync(toCurrency, fromCurrency, cancellationToken);
            if (inverse is > 0) rate = 1m / inverse.Value;
        }

        if (rate is not > 0)
        {
            throw new InvalidOperationException(
                $"Margin conversion {fromCurrency}/{toCurrency} is unavailable from Capital.com.");
        }

        _conversionCache[cacheKey] = new CachedConversion(rate.Value, _utcNow());
        return rate.Value;
    }

    private async Task<decimal?> FindMidpointAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken)
    {
        var markets = await _source.SearchMarketsAsync($"{fromCurrency}/{toCurrency}", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var market = markets.FirstOrDefault(item => IsCurrencyPair(item, fromCurrency, toCurrency));
        if (market is null || string.IsNullOrWhiteSpace(market.Epic)) return null;

        var details = await _source.GetMarketDetailsAsync(market.Epic, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return details?.Bid is > 0 && details.Offer is > 0
            ? (details.Bid.Value + details.Offer.Value) / 2m
            : null;
    }

    private static bool NeedsMarginMetadata(MarketInstrument instrument) =>
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

    private static bool IsCurrencyPair(MarketInstrument instrument, string fromCurrency, string toCurrency)
    {
        if (!instrument.Type.Contains("CURRENC", StringComparison.OrdinalIgnoreCase)) return false;
        var identity = NormalizePairIdentity($"{instrument.Symbol} {instrument.Name}");
        return identity.Contains(fromCurrency, StringComparison.OrdinalIgnoreCase) &&
               identity.Contains(toCurrency, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePairIdentity(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private sealed record CachedConversion(decimal Rate, DateTimeOffset RetrievedAt);
}
