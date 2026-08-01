namespace CAPETF.Desktop;

public sealed record SyntheticMarginLegPreview(
    string Side,
    string Epic,
    decimal ReferencePrice,
    decimal Quantity,
    decimal NativeNotional,
    string NativeCurrency,
    decimal NativeMargin,
    string AccountCurrency,
    decimal MarginAccountCurrency);

public sealed record SyntheticMarginSidePreview(
    string Side,
    string AccountCurrency,
    bool IsAvailable,
    string UnavailableReason,
    decimal? TotalMargin,
    IReadOnlyList<SyntheticMarginLegPreview> Legs);

public sealed record SyntheticMarginSummary(
    string AccountCurrency,
    decimal Available,
    decimal? AfterBuy,
    decimal? AfterSell,
    SyntheticMarginSidePreview Buy,
    SyntheticMarginSidePreview Sell,
    bool IsAccountStale = false,
    string AccountError = "");

public static class SyntheticMarginCalculator
{
    public static SyntheticMarginSidePreview CalculateSide(
        SyntheticBasket basket,
        string side,
        decimal basketNotional,
        string accountCurrency,
        decimal conversionRate)
    {
        var executable = SyntheticOrderSizing.BuildExecutableOrderPreview(basket, side, basketNotional);
        if (conversionRate <= 0)
        {
            return Unavailable(executable.Side, accountCurrency, $"Margin conversion to {accountCurrency} is unavailable.");
        }
        if (basket.Strategy == SyntheticStrategyKind.ManualFormula && basket.Components
                .Select(component => component.Instrument.Currency?.Trim() ?? "")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != 1)
        {
            return Unavailable(executable.Side, accountCurrency, "Manual basket components must use one currency.");
        }

        for (var index = 0; index < basket.Components.Count; index++)
        {
            var instrument = basket.Components[index].Instrument;
            if (instrument.MarginFactor is null ||
                !string.Equals(instrument.MarginFactorUnit, "PERCENTAGE", StringComparison.OrdinalIgnoreCase))
            {
                return Unavailable(executable.Side, accountCurrency, $"Margin factor for {instrument.Epic} is unavailable.");
            }
        }

        var legs = new List<SyntheticMarginLegPreview>(executable.Legs.Count);
        for (var index = 0; index < executable.Legs.Count; index++)
        {
            var leg = executable.Legs[index];
            var instrument = basket.Components[index].Instrument;
            var nativeNotional = leg.Notional;
            var nativeMargin = nativeNotional * instrument.MarginFactor!.Value / 100m;
            var accountMargin = nativeMargin * conversionRate;
            legs.Add(new SyntheticMarginLegPreview(
                leg.Side,
                leg.Epic,
                leg.ReferencePrice,
                leg.Quantity,
                nativeNotional,
                instrument.Currency,
                nativeMargin,
                accountCurrency,
                accountMargin));
        }

        return new SyntheticMarginSidePreview(
            executable.Side,
            accountCurrency,
            true,
            "",
            legs.Sum(leg => leg.MarginAccountCurrency),
            legs);
    }

    public static SyntheticMarginSummary Combine(
        CapitalAccountSnapshot account,
        SyntheticMarginSidePreview buy,
        SyntheticMarginSidePreview sell,
        bool isAccountStale = false,
        string accountError = "")
    {
        ValidateAccountCurrency(account, buy);
        ValidateAccountCurrency(account, sell);

        return new SyntheticMarginSummary(
            account.Currency,
            account.Available,
            buy.IsAvailable && buy.TotalMargin is decimal buyMargin ? account.Available - buyMargin : null,
            sell.IsAvailable && sell.TotalMargin is decimal sellMargin ? account.Available - sellMargin : null,
            buy,
            sell,
            isAccountStale,
            accountError);
    }

    private static SyntheticMarginSidePreview Unavailable(string side, string accountCurrency, string reason) =>
        new(side, accountCurrency, false, reason, null, []);

    private static void ValidateAccountCurrency(CapitalAccountSnapshot account, SyntheticMarginSidePreview preview)
    {
        if (!string.Equals(account.Currency, preview.AccountCurrency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Margin preview account currency {preview.AccountCurrency} does not match active account currency {account.Currency}.");
        }
    }
}
