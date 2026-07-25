namespace CAPETF.Desktop;

public static class CapitalInstrumentTypes
{
    public static bool IsStock(MarketInstrument instrument) =>
        string.Equals(instrument.Type?.Trim(), "SHARES", StringComparison.OrdinalIgnoreCase);
}
