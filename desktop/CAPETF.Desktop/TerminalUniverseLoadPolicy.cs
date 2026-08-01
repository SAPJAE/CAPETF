namespace CAPETF.Desktop;

public sealed class EtfCatalogCache
{
    private bool _attempted;

    public EtfDataLoadResult? Data { get; private set; }
    public IReadOnlySet<string> KnownEtfEpics { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public EtfDataLoadResult? LoadOnce(Func<EtfDataLoadResult> load)
    {
        if (_attempted) return Data;

        _attempted = true;
        try
        {
            Data = load();
            KnownEtfEpics = Data.KnownEtfEpics;
        }
        catch
        {
            Data = null;
            KnownEtfEpics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return Data;
    }
}

public static class TerminalUniverseLoadPolicy
{
    public static bool RequiresEtfMetadataEnrichment(
        TerminalUniverseKind universe,
        IReadOnlyList<MarketInstrument> instruments) =>
        universe == TerminalUniverseKind.ETFs && instruments.Any(EtfMetadataMerger.NeedsEnrichment);

    public static string ApiSearchTerm(TerminalUniverseKind universe, string searchText) =>
        universe switch
        {
            TerminalUniverseKind.ETFs => "ETF",
            TerminalUniverseKind.Crypto => "",
            _ => searchText,
        };

    public static IReadOnlyList<MarketInstrument> NormalizeApiFallback(
        TerminalUniverseKind universe,
        IReadOnlyList<MarketInstrument> markets,
        IReadOnlySet<string> knownEtfEpics)
    {
        if (universe == TerminalUniverseKind.Stocks)
        {
            return markets.Where(item => TerminalUniverse.Accepts(universe, item, knownEtfEpics)).ToList();
        }

        if (universe == TerminalUniverseKind.Crypto)
        {
            return markets
                .Where(item => TerminalUniverse.Accepts(universe, item, knownEtfEpics))
                .GroupBy(item => item.Epic, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        return markets
            .Where(item => IsEtfFallback(item, knownEtfEpics))
            .Select(CanonicalEtf)
            .Where(item => TerminalUniverse.Accepts(universe, item, knownEtfEpics))
            .ToList();
    }

    private static bool IsEtfFallback(MarketInstrument instrument, IReadOnlySet<string> knownEtfEpics)
    {
        if (knownEtfEpics.Contains(instrument.Epic) || CapitalInstrumentTypes.IsEtf(instrument)) return true;

        var name = instrument.Name.Trim();
        var paddedName = $" {name} ";
        return paddedName.Contains(" ETF", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("exchange traded", StringComparison.OrdinalIgnoreCase) ||
               paddedName.Contains(" UCITS", StringComparison.OrdinalIgnoreCase) ||
               instrument.Epic.EndsWith("ETF", StringComparison.OrdinalIgnoreCase);
    }

    private static MarketInstrument CanonicalEtf(MarketInstrument source)
    {
        var normalized = new MarketInstrument
        {
            Epic = source.Epic,
            Name = source.Name,
            Symbol = source.Symbol,
            Type = "ETF",
            Currency = source.Currency,
            Country = source.Country,
            Region = source.Region,
            Sector = source.Sector,
            Status = source.Status,
            Price = source.Price,
            Bid = source.Bid,
            Offer = source.Offer,
            ChangePercent = source.ChangePercent,
            LotSize = source.LotSize,
            MinDealSize = source.MinDealSize,
            MinSizeIncrement = source.MinSizeIncrement,
            MaxDealSize = source.MaxDealSize,
            MarketModes = source.MarketModes,
        };
        foreach (var point in source.Points) normalized.Points.Add(point);
        return normalized;
    }
}
