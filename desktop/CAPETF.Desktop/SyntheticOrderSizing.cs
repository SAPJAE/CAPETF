namespace CAPETF.Desktop;

public sealed record ExecutableLegPreview(decimal Quantity, decimal Notional, decimal WeightPct);

public static class SyntheticOrderSizing
{
    public static decimal DisplayMultiplier(SyntheticComponent component) =>
        decimal.Round(component.FormulaMultiplier, 2, MidpointRounding.AwayFromZero);

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

    public static string FormatDisplayMultiplier(SyntheticComponent component) =>
        DisplayMultiplier(component).ToString("0.##");

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
}
