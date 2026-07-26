namespace CAPETF.Desktop;

public static class SeedSearchOptionBuilder
{
    public static IReadOnlyList<string> BuildOptions(
        IReadOnlyList<MarketInstrument> instruments,
        string selectedBlock)
    {
        return instruments
            .Where(item => !string.IsNullOrWhiteSpace(item.Epic))
            .OrderBy(item => string.Equals(item.Group, selectedBlock, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Epic, StringComparer.OrdinalIgnoreCase)
            .SelectMany(OptionsForInstrument)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> OptionsForInstrument(MarketInstrument item)
    {
        var symbol = string.IsNullOrWhiteSpace(item.Symbol) ? item.Epic : item.Symbol.Trim();
        var name = string.IsNullOrWhiteSpace(item.Name) ? item.Epic : item.Name.Trim();
        var group = string.IsNullOrWhiteSpace(item.Group) ? "" : item.Group.Trim();
        var suffix = string.IsNullOrWhiteSpace(group) ? "" : $" | {group}";

        yield return $"{symbol} | {name}{suffix}";
        if (!string.Equals(symbol, name, StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{name} | {symbol}{suffix}";
        }
    }
}
