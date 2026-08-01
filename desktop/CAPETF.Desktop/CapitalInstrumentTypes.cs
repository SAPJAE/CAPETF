namespace CAPETF.Desktop;

public static class CapitalInstrumentTypes
{
    public static bool IsStock(MarketInstrument instrument) =>
        string.Equals(instrument.Type?.Trim(), "SHARES", StringComparison.OrdinalIgnoreCase);

    public static bool IsEtf(MarketInstrument instrument) =>
        instrument.Type?.Trim().ToUpperInvariant() is "ETF" or "ETFS" or "EXCHANGE TRADED FUND" or "EXCHANGE TRADED FUNDS";

    public static bool IsCrypto(MarketInstrument instrument) =>
        string.Equals(instrument.Type?.Trim(), "CRYPTOCURRENCIES", StringComparison.OrdinalIgnoreCase);
}
