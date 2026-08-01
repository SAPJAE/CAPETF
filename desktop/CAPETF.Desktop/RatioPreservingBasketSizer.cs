using System.Globalization;
using System.Numerics;

namespace CAPETF.Desktop;

public sealed class RatioPreservingBasketSizingException(string epic, string message)
    : InvalidOperationException(message)
{
    public string Epic { get; } = epic;
}

public static class RatioPreservingBasketSizer
{
    private static readonly BigInteger MaximumDecimalUnscaled = (BigInteger.One << 96) - BigInteger.One;

    public static decimal SmallestExecutableQuantity(IReadOnlyList<SyntheticComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);
        if (components.Count == 0)
        {
            throw Failure("", "At least one formula component is required.");
        }

        var increments = components.Select(BasketQuantityIncrement).ToArray();
        var sharedIncrement = increments.Aggregate(LeastCommonMultiple);
        var requiredMultiples = BigInteger.One;
        foreach (var component in components)
        {
            var quantityPerIncrement = Multiply(
                DecimalFraction(Math.Abs(component.FormulaMultiplier)),
                sharedIncrement);
            var minimum = DecimalFraction(component.Instrument.MinDealSize!.Value);
            requiredMultiples = BigInteger.Max(requiredMultiples, Ceiling(Divide(minimum, quantityPerIncrement)));
        }

        var basketQuantity = Multiply(sharedIncrement, new Fraction(requiredMultiples, BigInteger.One));
        var result = DecimalValue(
            basketQuantity,
            "",
            "Exact ratio-preserving basket quantity is unavailable within decimal limits.");
        ValidateExecutableQuantity(components, result);
        return result;
    }

    public static void ValidateExecutableQuantity(
        IReadOnlyList<SyntheticComponent> components,
        decimal basketQuantity)
    {
        ArgumentNullException.ThrowIfNull(components);
        if (basketQuantity <= 0m) throw Failure("", "Basket quantity must be positive.");

        var basketFraction = DecimalFraction(basketQuantity);
        foreach (var component in components)
        {
            ValidateComponent(component);
            var epic = component.Instrument.Epic;
            var quantityFraction = Multiply(
                DecimalFraction(Math.Abs(component.FormulaMultiplier)),
                basketFraction);
            var quantity = DecimalValue(
                quantityFraction,
                epic,
                "Exact ratio-preserving leg quantity is unavailable within decimal limits.");
            var minimum = component.Instrument.MinDealSize!.Value;
            var increment = component.Instrument.MinSizeIncrement!.Value;
            if (quantity < minimum)
            {
                throw Failure(epic,
                    $"Basket quantity {Format(basketQuantity)} produces {Format(quantity)}, below the {Format(minimum)} minimum deal size.");
            }

            var gridUnits = Divide(quantityFraction, DecimalFraction(increment));
            if (gridUnits.Denominator != BigInteger.One)
            {
                throw Failure(epic,
                    $"Basket quantity {Format(basketQuantity)} produces {Format(quantity)}, which is not on the {Format(increment)} size increment grid.");
            }
        }
    }

    private static Fraction BasketQuantityIncrement(SyntheticComponent component)
    {
        ValidateComponent(component);
        var ratio = Divide(
            DecimalFraction(Math.Abs(component.FormulaMultiplier)),
            DecimalFraction(component.Instrument.MinSizeIncrement!.Value));
        var numerator = ratio.Numerator;
        var terminatingFactor = BigInteger.One;
        while (numerator % 2 == 0)
        {
            numerator /= 2;
            terminatingFactor *= 2;
        }
        while (numerator % 5 == 0)
        {
            numerator /= 5;
            terminatingFactor *= 5;
        }

        return SmallestDecimalRepresentableMultiple(new Fraction(ratio.Denominator, terminatingFactor));
    }

    private static Fraction SmallestDecimalRepresentableMultiple(Fraction value)
    {
        var denominator = value.Denominator;
        var twos = 0;
        var fives = 0;
        while (denominator % 2 == 0)
        {
            denominator /= 2;
            twos++;
        }
        while (denominator % 5 == 0)
        {
            denominator /= 5;
            fives++;
        }
        if (denominator != BigInteger.One)
        {
            throw Failure("", "Exact ratio-preserving basket quantity is unavailable within decimal limits.");
        }

        var multiple = BigInteger.Pow(2, Math.Max(0, twos - 28)) *
                       BigInteger.Pow(5, Math.Max(0, fives - 28));
        return new Fraction(value.Numerator * multiple, value.Denominator);
    }

    private static Fraction LeastCommonMultiple(Fraction left, Fraction right) =>
        new(
            LeastCommonMultiple(left.Numerator, right.Numerator),
            BigInteger.GreatestCommonDivisor(left.Denominator, right.Denominator));

    private static BigInteger LeastCommonMultiple(BigInteger left, BigInteger right) =>
        BigInteger.Abs(left / BigInteger.GreatestCommonDivisor(left, right) * right);

    private static Fraction DecimalFraction(decimal value)
    {
        var bits = decimal.GetBits(value);
        var scale = (bits[3] >> 16) & 0xff;
        var unscaled = (BigInteger)(uint)bits[0] |
                       (BigInteger)(uint)bits[1] << 32 |
                       (BigInteger)(uint)bits[2] << 64;
        if ((bits[3] & int.MinValue) != 0) unscaled = -unscaled;
        return new Fraction(unscaled, BigInteger.Pow(10, scale));
    }

    private static decimal DecimalValue(Fraction value, string epic, string error)
    {
        var denominator = value.Denominator;
        var twos = 0;
        var fives = 0;
        while (denominator % 2 == 0)
        {
            denominator /= 2;
            twos++;
        }
        while (denominator % 5 == 0)
        {
            denominator /= 5;
            fives++;
        }

        var scale = Math.Max(twos, fives);
        if (denominator != BigInteger.One || scale > 28)
        {
            throw Failure(epic, error);
        }

        var unscaled = BigInteger.Abs(value.Numerator) *
                       BigInteger.Pow(2, scale - twos) *
                       BigInteger.Pow(5, scale - fives);
        if (unscaled > MaximumDecimalUnscaled)
        {
            throw Failure(epic, error);
        }

        var low = unchecked((int)(uint)(unscaled & uint.MaxValue));
        var middle = unchecked((int)(uint)((unscaled >> 32) & uint.MaxValue));
        var high = unchecked((int)(uint)((unscaled >> 64) & uint.MaxValue));
        return new decimal(low, middle, high, value.Numerator.Sign < 0, (byte)scale);
    }

    private static Fraction Multiply(Fraction left, Fraction right)
    {
        var leftNumerator = left.Numerator;
        var leftDenominator = left.Denominator;
        var rightNumerator = right.Numerator;
        var rightDenominator = right.Denominator;
        var firstDivisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(leftNumerator), rightDenominator);
        var secondDivisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(rightNumerator), leftDenominator);
        return new Fraction(
            leftNumerator / firstDivisor * (rightNumerator / secondDivisor),
            leftDenominator / secondDivisor * (rightDenominator / firstDivisor));
    }

    private static Fraction Divide(Fraction left, Fraction right)
    {
        if (right.Numerator.IsZero) throw new DivideByZeroException();
        return Multiply(left, new Fraction(right.Denominator, right.Numerator));
    }

    private static BigInteger Ceiling(Fraction value)
    {
        var quotient = BigInteger.DivRem(value.Numerator, value.Denominator, out var remainder);
        return remainder.IsZero ? quotient : quotient + BigInteger.One;
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

    private static string Format(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);

    private static RatioPreservingBasketSizingException Failure(string epic, string message) =>
        new(string.IsNullOrWhiteSpace(epic) ? "" : epic.Trim().ToUpperInvariant(), message);

    private readonly struct Fraction
    {
        public Fraction(BigInteger numerator, BigInteger denominator)
        {
            if (denominator.IsZero) throw new DivideByZeroException();
            if (denominator.Sign < 0)
            {
                numerator = -numerator;
                denominator = -denominator;
            }
            var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
            Numerator = numerator / divisor;
            Denominator = denominator / divisor;
        }

        public BigInteger Numerator { get; }
        public BigInteger Denominator { get; }
    }
}
