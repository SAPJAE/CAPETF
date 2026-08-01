using System.Globalization;

namespace CAPETF.Desktop;

public sealed class RatioPreservingBasketSizingException(string epic, string message)
    : InvalidOperationException(message)
{
    public string Epic { get; } = epic;
}

public static class RatioPreservingBasketSizer
{
    public static decimal SmallestExecutableQuantity(IReadOnlyList<SyntheticComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);
        if (components.Count == 0)
        {
            throw Failure("", "At least one formula component is required.");
        }

        var increments = components.Select(BasketQuantityIncrement).ToArray();
        var sharedIncrement = LeastCommonDecimalMultiple(increments);
        if (sharedIncrement <= 0m)
        {
            throw Failure("", "Exact ratio-preserving basket quantity is unavailable within decimal limits.");
        }

        decimal requiredMultiples = 1m;
        for (var index = 0; index < components.Count; index++)
        {
            var component = components[index];
            var minimum = component.Instrument.MinDealSize!.Value;
            decimal quantityPerIncrement;
            try
            {
                quantityPerIncrement = checked(Math.Abs(component.FormulaMultiplier) * sharedIncrement);
            }
            catch (OverflowException)
            {
                throw Failure(component.Instrument.Epic,
                    "Exact ratio-preserving basket quantity is unavailable within decimal limits.");
            }

            var multiples = decimal.Truncate(minimum / quantityPerIncrement);
            if (multiples * quantityPerIncrement < minimum) multiples += 1m;
            requiredMultiples = Math.Max(requiredMultiples, multiples);
        }

        decimal basketQuantity;
        try
        {
            basketQuantity = checked(sharedIncrement * requiredMultiples);
        }
        catch (OverflowException)
        {
            throw Failure("", "Exact ratio-preserving basket quantity is unavailable within decimal limits.");
        }

        ValidateExecutableQuantity(components, basketQuantity);
        return basketQuantity;
    }

    public static void ValidateExecutableQuantity(
        IReadOnlyList<SyntheticComponent> components,
        decimal basketQuantity)
    {
        ArgumentNullException.ThrowIfNull(components);
        if (basketQuantity <= 0m) throw Failure("", "Basket quantity must be positive.");

        foreach (var component in components)
        {
            ValidateComponent(component);
            decimal quantity;
            try
            {
                quantity = checked(Math.Abs(component.FormulaMultiplier) * basketQuantity);
            }
            catch (OverflowException)
            {
                throw Failure(component.Instrument.Epic,
                    "Exact ratio-preserving leg quantity is unavailable within decimal limits.");
            }

            var minimum = component.Instrument.MinDealSize!.Value;
            var increment = component.Instrument.MinSizeIncrement!.Value;
            if (quantity < minimum)
            {
                throw Failure(component.Instrument.Epic,
                    $"Basket quantity {Format(basketQuantity)} produces {Format(quantity)}, below the {Format(minimum)} minimum deal size.");
            }
            if (quantity % increment != 0m)
            {
                throw Failure(component.Instrument.Epic,
                    $"Basket quantity {Format(basketQuantity)} produces {Format(quantity)}, which is not on the {Format(increment)} size increment grid.");
            }
        }
    }

    private static decimal BasketQuantityIncrement(SyntheticComponent component)
    {
        ValidateComponent(component);
        var (multiplier, multiplierScale) = Unscaled(Math.Abs(component.FormulaMultiplier));
        var (increment, incrementScale) = Unscaled(component.Instrument.MinSizeIncrement!.Value);
        var numerator = multiplier;
        var denominator = increment;
        ScaleFraction(ref numerator, ref denominator, incrementScale - multiplierScale, component.Instrument.Epic);

        var divisor = GreatestCommonDivisor(numerator, denominator);
        numerator /= divisor;
        denominator /= divisor;

        decimal terminatingFactor = 1m;
        while (numerator % 2m == 0m)
        {
            numerator /= 2m;
            terminatingFactor = checked(terminatingFactor * 2m);
        }
        while (numerator % 5m == 0m)
        {
            numerator /= 5m;
            terminatingFactor = checked(terminatingFactor * 5m);
        }

        var result = denominator / terminatingFactor;
        if (result <= 0m)
        {
            throw Failure(component.Instrument.Epic,
                "Exact ratio-preserving basket quantity is unavailable within decimal limits.");
        }
        return result;
    }

    private static decimal LeastCommonDecimalMultiple(IReadOnlyList<decimal> values)
    {
        var normalized = values.Select(Unscaled).ToArray();
        var commonScale = normalized.Max(value => value.Scale);
        var scaled = normalized.Select(value => ScaleInteger(value.Value, commonScale - value.Scale, "")).ToArray();
        var multiple = scaled[0];
        for (var index = 1; index < scaled.Length; index++)
        {
            var divisor = GreatestCommonDivisor(multiple, scaled[index]);
            try
            {
                multiple = checked(multiple / divisor * scaled[index]);
            }
            catch (OverflowException)
            {
                throw Failure("", "Exact ratio-preserving basket quantity is unavailable within decimal limits.");
            }
        }

        for (var index = 0; index < commonScale; index++) multiple /= 10m;
        return multiple;
    }

    private static void ValidateComponent(SyntheticComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        var epic = component.Instrument.Epic;
        if (component.FormulaMultiplier == 0m)
        {
            throw Failure(epic, "Formula multiplier must be non-zero.");
        }
        if (component.Instrument.MinDealSize is not > 0m || component.Instrument.MinSizeIncrement is not > 0m)
        {
            throw Failure(epic, "Minimum deal size and size increment must be positive.");
        }
    }

    private static void ScaleFraction(
        ref decimal numerator,
        ref decimal denominator,
        int scaleDifference,
        string epic)
    {
        if (scaleDifference > 0)
        {
            for (var index = 0; index < scaleDifference; index++)
            {
                var divisor = GreatestCommonDivisor(denominator, 10m);
                denominator /= divisor;
                numerator = ScaleInteger(numerator, 1, epic, 10m / divisor);
            }
        }
        else
        {
            for (var index = 0; index < -scaleDifference; index++)
            {
                var divisor = GreatestCommonDivisor(numerator, 10m);
                numerator /= divisor;
                denominator = ScaleInteger(denominator, 1, epic, 10m / divisor);
            }
        }
    }

    private static decimal ScaleInteger(decimal value, int places, string epic, decimal factor = 10m)
    {
        try
        {
            for (var index = 0; index < places; index++) value = checked(value * factor);
            return value;
        }
        catch (OverflowException)
        {
            throw Failure(epic, "Exact ratio-preserving basket quantity is unavailable within decimal limits.");
        }
    }

    private static (decimal Value, int Scale) Unscaled(decimal value)
    {
        var bits = decimal.GetBits(value);
        var scale = (bits[3] >> 16) & 0x7f;
        var unscaled = new decimal(bits[0], bits[1], bits[2], false, 0);
        while (scale > 0 && unscaled % 10m == 0m)
        {
            unscaled /= 10m;
            scale--;
        }
        return (unscaled, scale);
    }

    private static decimal GreatestCommonDivisor(decimal left, decimal right)
    {
        while (right != 0m)
        {
            var remainder = left % right;
            left = right;
            right = remainder;
        }
        return Math.Abs(left);
    }

    private static string Format(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);

    private static RatioPreservingBasketSizingException Failure(string epic, string message) =>
        new(string.IsNullOrWhiteSpace(epic) ? "" : epic.Trim().ToUpperInvariant(), message);
}
