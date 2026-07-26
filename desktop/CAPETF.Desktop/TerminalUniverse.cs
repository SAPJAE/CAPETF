namespace CAPETF.Desktop;

public enum TerminalUniverseKind
{
    Stocks,
    ETFs,
}

public static class TerminalUniverse
{
    public static bool Accepts(TerminalUniverseKind kind, MarketInstrument instrument) =>
        IsOpenEligible(instrument) && kind switch
        {
            TerminalUniverseKind.Stocks => CapitalInstrumentTypes.IsStock(instrument),
            TerminalUniverseKind.ETFs => CapitalInstrumentTypes.IsEtf(instrument),
            _ => false,
        };

    private static bool IsOpenEligible(MarketInstrument instrument)
    {
        var status = instrument.Status.Trim();
        if (string.IsNullOrWhiteSpace(status)) return true;

        var normalized = status.Replace('_', ' ').Replace('-', ' ').Trim();
        foreach (var blocked in new[]
        {
            "CLOSE ONLY", "CLOSING ONLY", "CLOSINGS ONLY", "VIEW ONLY", "REDUCE ONLY",
            "DISABLED", "SUSPENDED", "DELISTED", "EXPIRED", "UNAVAILABLE", "NOT AVAILABLE",
            "NOT TRADEABLE", "NOT TRADABLE", "CANNOT OPEN", "OPEN DISABLED", "OPENING DISABLED", "OBSOLETE",
        })
        {
            if (normalized.Contains(blocked, StringComparison.OrdinalIgnoreCase)) return false;
        }

        return normalized.Equals("OPEN", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("CLOSED", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("TRADEABLE", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("TRADABLE", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("NORMAL", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("ONLINE", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
    }
}
