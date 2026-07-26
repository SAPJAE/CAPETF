namespace CAPETF.Desktop;

public static class EtfMetadataMerger
{
    public static MarketInstrument Merge(MarketInstrument cached, MarketInstrument details)
    {
        var country = First(cached.Country, details.Country);
        var merged = new MarketInstrument
        {
            Epic = First(cached.Epic, details.Epic),
            Name = First(cached.Name, details.Name),
            Symbol = First(cached.Symbol, details.Symbol),
            Type = First(cached.Type, details.Type),
            Currency = First(cached.Currency, details.Currency),
            Country = country,
            Region = FirstMeaningful(cached.Region, details.Region, RegionFromCountry(country)),
            Sector = FirstMeaningful(cached.Sector, details.Sector, "All"),
            Status = First(details.Status, cached.Status),
            Price = cached.Price ?? details.Price,
            Bid = cached.Bid ?? details.Bid,
            Offer = cached.Offer ?? details.Offer,
            ChangePercent = cached.ChangePercent ?? details.ChangePercent,
            LotSize = cached.LotSize ?? details.LotSize,
            MinDealSize = cached.MinDealSize ?? details.MinDealSize,
            MinSizeIncrement = cached.MinSizeIncrement ?? details.MinSizeIncrement,
        };

        foreach (var point in cached.Points.Count > 0 ? cached.Points : details.Points) merged.Points.Add(point);
        return merged;
    }

    public static bool NeedsEnrichment(MarketInstrument instrument) =>
        string.IsNullOrWhiteSpace(instrument.Currency) ||
        string.IsNullOrWhiteSpace(instrument.Country) ||
        string.IsNullOrWhiteSpace(instrument.Region) ||
        string.Equals(instrument.Region, "Other", StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrWhiteSpace(instrument.Sector) ||
        string.Equals(instrument.Sector, "All", StringComparison.OrdinalIgnoreCase);

    private static string First(string first, string second) =>
        !string.IsNullOrWhiteSpace(first) ? first : second;

    private static string FirstMeaningful(string first, string second, string fallback) =>
        !string.IsNullOrWhiteSpace(first) && !string.Equals(first, fallback, StringComparison.OrdinalIgnoreCase)
            ? first
            : !string.IsNullOrWhiteSpace(second)
                ? second
                : fallback;

    private static string RegionFromCountry(string country) =>
        country.Contains("United States", StringComparison.OrdinalIgnoreCase) ? "US" : "Other";
}
