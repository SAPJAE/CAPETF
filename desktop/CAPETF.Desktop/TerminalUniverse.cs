namespace CAPETF.Desktop;

public enum TerminalUniverseKind
{
    Stocks,
    ETFs,
    Crypto,
}

public static class TerminalUniverse
{
    public static bool Accepts(TerminalUniverseKind kind, MarketInstrument instrument) =>
        Accepts(kind, instrument, knownEtfEpics: null);

    public static bool Accepts(
        TerminalUniverseKind kind,
        MarketInstrument instrument,
        IReadOnlySet<string>? knownEtfEpics) =>
        IsOpenEligible(instrument) && kind switch
        {
            TerminalUniverseKind.Stocks => CapitalInstrumentTypes.IsStock(instrument) && !IsKnownEtf(instrument, knownEtfEpics),
            TerminalUniverseKind.ETFs => CapitalInstrumentTypes.IsEtf(instrument) || IsKnownEtf(instrument, knownEtfEpics),
            TerminalUniverseKind.Crypto => CapitalInstrumentTypes.IsCrypto(instrument),
            _ => false,
        };

    private static bool IsKnownEtf(MarketInstrument instrument, IReadOnlySet<string>? knownEtfEpics) =>
        knownEtfEpics is not null && !string.IsNullOrWhiteSpace(instrument.Epic) && knownEtfEpics.Contains(instrument.Epic);

    private static bool IsOpenEligible(MarketInstrument instrument)
    {
        if (instrument.MarketModes.Any(IsBlockedMode)) return false;
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

    internal static bool IsBlockedMode(string mode)
    {
        var normalized = (mode ?? "").Replace('_', ' ').Replace('-', ' ').Trim();
        return normalized.Contains("CLOSE ONLY", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("VIEW ONLY", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("REDUCE ONLY", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("OPEN DISABLED", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("CANNOT OPEN", StringComparison.OrdinalIgnoreCase);
    }
}
