using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CAPETF.Desktop;

public sealed record SavedSyntheticComponent(
    string Epic,
    string Name,
    string Currency,
    decimal Weight,
    decimal FormulaMultiplier,
    decimal? ReferencePrice);

public sealed record SavedSyntheticBasket(
    string Id,
    string Name,
    string Symbol,
    string Block,
    SyntheticStrategyKind Strategy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<SavedSyntheticComponent> Components,
    decimal? BasketQuantity = null,
    TerminalUniverseKind? UniverseKind = null)
{
    [JsonIgnore]
    public string DisplayLabel => Strategy == SyntheticStrategyKind.ManualFormula
        ? $"{Name} | {ManualSyntheticFormula.Format(Components)}"
        : Name;

    public static SavedSyntheticBasket FromBasket(
        string name,
        SyntheticStrategyKind strategy,
        SyntheticBasket basket,
        TerminalUniverseKind? universeKind = null)
    {
        var now = DateTimeOffset.UtcNow;
        var componentIdentity = string.Join("|", basket.Components.Select(component => component.Instrument.Epic));
        var id = strategy == SyntheticStrategyKind.ManualFormula
            ? ManualFormulaId(basket.Symbol, basket.Components)
            : StableId($"{basket.Symbol}-{componentIdentity}");
        return new SavedSyntheticBasket(
            id,
            string.IsNullOrWhiteSpace(name) ? basket.Symbol : name.Trim(),
            basket.Symbol,
            basket.Block,
            strategy,
            now,
            now,
            basket.Components.Select(component => new SavedSyntheticComponent(
                component.Instrument.Epic,
                component.Instrument.Name,
                string.IsNullOrWhiteSpace(component.Instrument.Currency) ? "" : component.Instrument.Currency.Trim(),
                component.Weight,
                component.FormulaMultiplier,
                component.FormulaReferencePrice)).ToList(),
            UniverseKind: universeKind ?? basket.UniverseKind);
    }

    private static string ManualFormulaId(string symbol, IEnumerable<SyntheticComponent> components)
    {
        var source = symbol.Trim().ToUpperInvariant() + "|" + string.Join("|",
            components.Select(component => string.Join(":",
                component.Instrument.Epic.Trim().ToUpperInvariant(),
                component.FormulaMultiplier.ToString("G29", CultureInfo.InvariantCulture))));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        return $"MANUAL-{hash[..24]}";
    }

    private static string StableId(string source)
    {
        var chars = source
            .ToUpperInvariant()
            .Where(character => char.IsLetterOrDigit(character) || character == '-')
            .Take(80)
            .ToArray();
        return chars.Length == 0 ? Guid.NewGuid().ToString("N") : new string(chars);
    }
}

public sealed class SavedSyntheticBasketStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SavedSyntheticBasketStore(string? folder = null)
    {
        var root = string.IsNullOrWhiteSpace(folder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CAPETF")
            : folder;
        Directory.CreateDirectory(root);
        _filePath = Path.Combine(root, "saved-synthetics.json");
    }

    public IReadOnlyList<SavedSyntheticBasket> LoadAll()
    {
        if (!File.Exists(_filePath)) return [];
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<SavedSyntheticBasket>>(json, JsonOptions) ?? [];
    }

    public void Save(SavedSyntheticBasket basket)
    {
        var existing = LoadAll()
            .Where(item => !string.Equals(item.Id, basket.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        existing.Add(basket with { UpdatedAt = DateTimeOffset.UtcNow });
        var ordered = existing
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToList();
        File.WriteAllText(_filePath, JsonSerializer.Serialize(ordered, JsonOptions));
    }

    public bool Delete(string id)
    {
        var existing = LoadAll().ToList();
        var remaining = existing
            .Where(item => !string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (remaining.Count == existing.Count) return false;

        File.WriteAllText(_filePath, JsonSerializer.Serialize(remaining, JsonOptions));
        return true;
    }
}
