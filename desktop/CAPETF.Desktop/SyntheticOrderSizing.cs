using System.Globalization;

namespace CAPETF.Desktop;

public sealed record ExecutableLegPreview(decimal Quantity, decimal Notional, decimal WeightPct);

public sealed record ExecutableOrderLegPreview(
    string Side,
    string Epic,
    decimal ReferencePrice,
    decimal Quantity,
    decimal Notional,
    decimal TargetWeightPct,
    decimal ActualWeightPct,
    decimal WeightImbalancePct);

public sealed record ExecutableOrderPreview(
    string Side,
    decimal RequestedBasketNotional,
    decimal TotalExecutableNotional,
    decimal MaxAbsoluteWeightImbalancePct,
    IReadOnlyList<ExecutableOrderLegPreview> Legs);

public static class SyntheticOrderSizing
{
    public static decimal DisplayMultiplier(SyntheticComponent component) => component.FormulaMultiplier;

    public static decimal ExecutableLegQuantity(SyntheticComponent component, decimal syntheticQuantity)
    {
        var raw = Math.Abs(syntheticQuantity * component.FormulaMultiplier);
        return decimal.Round(RoundUpToDealRules(component, raw), 4, MidpointRounding.AwayFromZero);
    }

    public static ExecutableLegPreview ExecutableLegPreview(
        SyntheticComponent component,
        decimal basketNotional,
        decimal referencePrice)
    {
        if (basketNotional <= 0) throw new ArgumentOutOfRangeException(nameof(basketNotional));
        if (referencePrice <= 0) throw new ArgumentOutOfRangeException(nameof(referencePrice));

        var targetLegNotional = basketNotional * component.Weight / 100m;
        var quantity = RoundUpToDealRules(component, targetLegNotional / referencePrice);
        var notional = quantity * referencePrice;
        var weightPct = notional / basketNotional * 100m;
        return new ExecutableLegPreview(quantity, notional, weightPct);
    }

    public static ExecutableOrderPreview BuildExecutableOrderPreview(
        SyntheticBasket basket,
        string side,
        decimal basketNotional)
    {
        var normalizedSide = side.Equals("SELL", StringComparison.OrdinalIgnoreCase) ? "SELL" : "BUY";
        if (basketNotional <= 0) throw new ArgumentOutOfRangeException(nameof(basketNotional));
        if (basket.Components.Count == 0) throw new InvalidOperationException("Build a synthetic basket first.");

        var sized = basket.Components.Select(component =>
        {
            var legSide = component.FormulaMultiplier >= 0 ? normalizedSide : Opposite(normalizedSide);
            var referencePrice = legSide == "BUY" ? component.Instrument.Offer : component.Instrument.Bid;
            if (referencePrice is not > 0)
            {
                throw new InvalidOperationException($"{component.Instrument.Epic} {legSide.ToLowerInvariant()} price is unavailable.");
            }

            return (component, legSide, referencePrice: referencePrice.Value,
                preview: ExecutableLegPreview(component, basketNotional, referencePrice.Value));
        }).ToList();
        var totalNotional = sized.Sum(item => item.preview.Notional);
        var legs = sized.Select(item =>
        {
            var actualWeight = totalNotional <= 0 ? 0 : item.preview.Notional / totalNotional * 100m;
            return new ExecutableOrderLegPreview(
                item.legSide,
                item.component.Instrument.Epic,
                item.referencePrice,
                item.preview.Quantity,
                item.preview.Notional,
                item.component.Weight,
                actualWeight,
                actualWeight - item.component.Weight);
        }).ToList();

        return new ExecutableOrderPreview(
            normalizedSide,
            basketNotional,
            totalNotional,
            legs.Max(leg => Math.Abs(leg.WeightImbalancePct)),
            legs);
    }

    public static string FormatDisplayMultiplier(SyntheticComponent component) =>
        DisplayMultiplier(component).ToString("G8", CultureInfo.InvariantCulture);

    private static decimal RoundUpToDealRules(SyntheticComponent component, decimal raw)
    {
        var minDealSize = PositiveOrNull(component.Instrument.MinDealSize);
        var step = PositiveOrNull(component.Instrument.MinSizeIncrement);
        var sized = minDealSize is not null && raw < minDealSize.Value ? minDealSize.Value : raw;

        if (step is not null)
        {
            sized = Math.Ceiling(sized / step.Value) * step.Value;
        }

        return sized;
    }

    private static decimal? PositiveOrNull(decimal? value) => value is > 0 ? value : null;

    private static string Opposite(string side) => side == "BUY" ? "SELL" : "BUY";
}
