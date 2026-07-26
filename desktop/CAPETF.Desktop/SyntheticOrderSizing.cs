namespace CAPETF.Desktop;

public static class SyntheticOrderSizing
{
    public static decimal DisplayMultiplier(SyntheticComponent component) =>
        decimal.Round(component.FormulaMultiplier, 2, MidpointRounding.AwayFromZero);

    public static decimal ExecutableLegQuantity(SyntheticComponent component, decimal syntheticQuantity)
    {
        var raw = Math.Abs(syntheticQuantity * component.FormulaMultiplier);
        var minDealSize = PositiveOrNull(component.Instrument.MinDealSize);
        var step = PositiveOrNull(component.Instrument.MinSizeIncrement);
        var sized = minDealSize is not null && raw < minDealSize.Value ? minDealSize.Value : raw;

        if (step is not null)
        {
            sized = Math.Ceiling(sized / step.Value) * step.Value;
        }

        return decimal.Round(sized, 4, MidpointRounding.AwayFromZero);
    }

    public static string FormatDisplayMultiplier(SyntheticComponent component) =>
        DisplayMultiplier(component).ToString("0.##");

    private static decimal? PositiveOrNull(decimal? value) => value is > 0 ? value : null;
}
