namespace CAPETF.Desktop;

public static class SyntheticTradePreflight
{
    private static readonly TimeSpan MaximumQuoteAge = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(2);

    public static SyntheticPreflightResult Build(SyntheticPreflightInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Basket);

        var globalFailures = new List<SyntheticPreflightFailure>();
        var legFailures = new List<SyntheticPreflightFailure>();
        var side = NormalizeSide(input.Side);
        var isManual = input.Basket.Strategy == SyntheticStrategyKind.ManualFormula;

        if (!input.IsDemoSession) globalFailures.Add(Failure("Demo trading session is required."));
        if (string.IsNullOrWhiteSpace(input.AccountId)) globalFailures.Add(Failure("Active Capital.com account is required."));
        if (!input.HedgingMode) globalFailures.Add(Failure("Capital.com hedging mode is required."));
        if (string.IsNullOrWhiteSpace(input.BasketId)) globalFailures.Add(Failure("Basket ID is required."));
        if (input.RequestedNotional <= 0m) globalFailures.Add(Failure("Requested notional must be positive."));
        if (side is null) globalFailures.Add(Failure("Side must be BUY or SELL."));
        var validComponentCount = isManual
            ? input.Basket.Components.Count is >= 2 and <= 4
            : input.Basket.Components.Count is >= 3 and <= 4;
        if (!validComponentCount)
        {
            globalFailures.Add(Failure(isManual
                ? "Manual synthetic baskets must contain 2 to 4 components."
                : "Synthetic baskets must contain 3 or 4 components."));
        }

        var components = input.Basket.Components
            .OrderBy(component => component.Instrument.Epic, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var duplicate in components
                     .Where(component => !string.IsNullOrWhiteSpace(component.Instrument.Epic))
                     .GroupBy(component => component.Instrument.Epic.Trim(), StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            legFailures.Add(Failure(NormalizeEpic(duplicate.Key), "Duplicate epic."));
        }
        if (isManual)
        {
            var currencies = components
                .Select(component => component.Instrument.Currency?.Trim() ?? "")
                .Where(currency => !string.IsNullOrWhiteSpace(currency))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (currencies.Length != 1 || components.Any(component => string.IsNullOrWhiteSpace(component.Instrument.Currency)))
            {
                globalFailures.Add(Failure("Manual basket components must use one currency."));
            }
        }

        foreach (var component in components)
        {
            var epic = NormalizeEpic(component.Instrument.Epic);
            if (string.IsNullOrWhiteSpace(component.Instrument.Epic))
            {
                legFailures.Add(Failure(epic, "Epic is required."));
            }
            if (!string.Equals(component.Instrument.Status?.Trim(), "TRADEABLE", StringComparison.OrdinalIgnoreCase))
            {
                legFailures.Add(Failure(epic, "Market is not TRADEABLE."));
            }
            if (component.Instrument.Bid is not > 0m || component.Instrument.Offer is not > 0m)
            {
                legFailures.Add(Failure(epic, "Bid and offer prices must be positive."));
            }
            if (component.Instrument.MarketModes.Any(TerminalUniverse.IsBlockedMode))
            {
                legFailures.Add(Failure(epic, "Market mode does not allow opening a position."));
            }
            if (isManual &&
                (component.Instrument.MinDealSize is not > 0m || component.Instrument.MinSizeIncrement is not > 0m))
            {
                legFailures.Add(Failure(epic, "Minimum deal size and size increment must be positive."));
            }
            if (isManual && component.Instrument.MaxDealSize is not > 0m)
            {
                legFailures.Add(Failure(epic, "Maximum deal size must be positive."));
            }
            if (component.Instrument.LastTickAt is not null && component.Instrument.LastTickAt.Value > input.NowUtc)
            {
                legFailures.Add(Failure(epic, "Quote timestamp is in the future."));
            }
            else if (component.Instrument.LastTickAt is null ||
                     input.NowUtc - component.Instrument.LastTickAt.Value > MaximumQuoteAge)
            {
                legFailures.Add(Failure(epic, "Quote is older than five minutes."));
            }
        }

        ExecutableOrderPreview? executable = null;
        var hasUsableManualRules = !isManual || input.Basket.Components.All(component =>
            component.Instrument.MinDealSize is > 0m && component.Instrument.MinSizeIncrement is > 0m &&
            component.Instrument.MaxDealSize is > 0m);
        if (input.RequestedNotional > 0m && input.Basket.Components.Count > 0 && hasUsableManualRules &&
            input.Basket.Components.All(component => component.Instrument.Bid is > 0m && component.Instrument.Offer is > 0m))
        {
            try
            {
                executable = SyntheticOrderSizing.BuildExecutableOrderPreview(input.Basket, side ?? "BUY", input.RequestedNotional);
                for (var index = 0; index < executable.Legs.Count; index++)
                {
                    if (!IsValidRoundedSize(executable.Legs[index].Quantity, input.Basket.Components[index].Instrument))
                    {
                        legFailures.Add(Failure(NormalizeEpic(executable.Legs[index].Epic), "Rounded size is invalid."));
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                globalFailures.Add(Failure("Executable order sizing is unavailable."));
            }
            catch (RatioPreservingBasketSizingException exception)
            {
                if (string.IsNullOrWhiteSpace(exception.Epic)) globalFailures.Add(Failure(exception.Message));
                else legFailures.Add(Failure(exception.Epic, exception.Message));
            }
            catch (InvalidOperationException)
            {
                globalFailures.Add(Failure("Executable order sizing is unavailable."));
            }
        }

        var margin = side is null ? null : MarginForSide(input.Margin, side);
        var manualMarginIsConsistent = !isManual ||
            (executable is not null && IsValidManualMarginSnapshot(input.Margin, input.Basket, input.RequestedNotional));
        if (margin is null || !margin.IsAvailable || margin.TotalMargin is null || input.Margin?.IsAccountStale == true)
        {
            globalFailures.Add(Failure("Margin preview is unavailable."));
        }
        else if (!manualMarginIsConsistent)
        {
            globalFailures.Add(Failure("Margin preview is inconsistent."));
        }
        else
        {
            if (margin.TotalMargin.Value > input.Margin!.Available)
            {
                globalFailures.Add(Failure("Estimated margin exceeds available funds."));
            }
            if (executable is not null)
            {
                foreach (var leg in executable.Legs)
                {
                    if (FindMarginLeg(margin, leg, isManual) is null)
                    {
                        legFailures.Add(Failure(NormalizeEpic(leg.Epic), "Margin is unavailable."));
                    }
                }
            }
        }

        var failures = globalFailures
            .Concat(legFailures
                .OrderBy(failure => failure.Epic, StringComparer.OrdinalIgnoreCase)
                .ThenBy(failure => failure.Reason, StringComparer.Ordinal))
            .ToArray();
        if (failures.Length > 0 || executable is null || margin?.TotalMargin is null)
        {
            return new SyntheticPreflightResult(false, null, Array.AsReadOnly(failures));
        }

        var legs = executable.Legs
            .Select(leg =>
            {
                var marginLeg = FindMarginLeg(margin, leg, isManual)!;
                var component = input.Basket.Components.Single(component =>
                    string.Equals(component.Instrument.Epic, leg.Epic, StringComparison.OrdinalIgnoreCase));
                return new SyntheticExecutionLeg(
                    leg.Epic,
                    leg.Side,
                    component.FormulaMultiplier,
                    leg.ReferencePrice,
                    leg.Quantity,
                    leg.Notional,
                    marginLeg.MarginAccountCurrency,
                    marginLeg.AccountCurrency);
            })
            .ToArray();
        var ticket = new SyntheticExecutionTicket(
            Guid.NewGuid().ToString("N"),
            input.BasketId,
            executable.Side,
            input.RequestedNotional,
            input.NowUtc,
            input.NowUtc.Add(TicketLifetime),
            margin.TotalMargin.Value,
            margin.AccountCurrency,
            Array.AsReadOnly(legs),
            input.AccountId,
            executable.BasketQuantity,
            input.UniverseKind ?? input.Basket.UniverseKind);

        return new SyntheticPreflightResult(true, ticket, Array.Empty<SyntheticPreflightFailure>());
    }

    private static bool IsValidManualMarginSnapshot(
        SyntheticMarginSummary? summary,
        SyntheticBasket basket,
        decimal basketQuantity)
    {
        if (summary is null ||
            summary.Buy is null ||
            summary.Sell is null ||
            summary.IsAccountStale ||
            !string.IsNullOrWhiteSpace(summary.AccountError) ||
            string.IsNullOrWhiteSpace(summary.AccountCurrency))
        {
            return false;
        }

        try
        {
            var buy = SyntheticOrderSizing.BuildExecutableOrderPreview(basket, "BUY", basketQuantity);
            var sell = SyntheticOrderSizing.BuildExecutableOrderPreview(basket, "SELL", basketQuantity);
            if (!IsValidManualMarginSide(summary.Buy, buy, basket, summary.AccountCurrency) ||
                !IsValidManualMarginSide(summary.Sell, sell, basket, summary.AccountCurrency) ||
                !SameCurrency(summary.Buy.NativeCurrency, summary.Sell.NativeCurrency) ||
                summary.Buy.ConversionRate != summary.Sell.ConversionRate)
            {
                return false;
            }

            return summary.Buy.TotalMargin is decimal buyMargin &&
                   summary.Sell.TotalMargin is decimal sellMargin &&
                   summary.AfterBuy == summary.Available - buyMargin &&
                   summary.AfterSell == summary.Available - sellMargin;
        }
        catch (ArithmeticException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsValidManualMarginSide(
        SyntheticMarginSidePreview margin,
        ExecutableOrderPreview executable,
        SyntheticBasket basket,
        string accountCurrency)
    {
        if (margin is null ||
            margin.Legs is null ||
            !margin.IsAvailable ||
            !string.IsNullOrWhiteSpace(margin.UnavailableReason) ||
            margin.TotalMargin is not decimal totalMargin ||
            margin.ConversionRate is not > 0m ||
            totalMargin < 0m ||
            !string.Equals(margin.Side, executable.Side, StringComparison.OrdinalIgnoreCase) ||
            !SameCurrency(margin.AccountCurrency, accountCurrency) ||
            margin.Legs.Count != executable.Legs.Count)
        {
            return false;
        }

        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var leg in margin.Legs)
        {
            if (leg is null || string.IsNullOrWhiteSpace(leg.Epic) || string.IsNullOrWhiteSpace(leg.Side)) return false;
            if (!identities.Add($"{leg.Epic.Trim()}|{leg.Side.Trim()}")) return false;
        }

        decimal marginSum = 0m;
        foreach (var executableLeg in executable.Legs)
        {
            var matches = margin.Legs.Where(candidate =>
                string.Equals(candidate.Epic, executableLeg.Epic, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(candidate.Side, executableLeg.Side, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) return false;

            var marginLeg = matches[0];
            var component = basket.Components.Single(component =>
                string.Equals(component.Instrument.Epic, executableLeg.Epic, StringComparison.OrdinalIgnoreCase));
            if (component.Instrument.MarginFactor is not > 0m ||
                !string.Equals(component.Instrument.MarginFactorUnit, "PERCENTAGE", StringComparison.OrdinalIgnoreCase) ||
                !SameCurrency(margin.NativeCurrency, component.Instrument.Currency) ||
                !SameCurrency(marginLeg.NativeCurrency, component.Instrument.Currency) ||
                !SameCurrency(marginLeg.AccountCurrency, accountCurrency) ||
                marginLeg.ReferencePrice != executableLeg.ReferencePrice ||
                marginLeg.Quantity != executableLeg.Quantity ||
                marginLeg.NativeNotional != executableLeg.Notional)
            {
                return false;
            }

            var expectedNativeMargin = executableLeg.Notional * component.Instrument.MarginFactor.Value / 100m;
            if (marginLeg.NativeMargin != expectedNativeMargin ||
                marginLeg.NativeMargin <= 0m ||
                marginLeg.MarginAccountCurrency <= 0m ||
                marginLeg.MarginAccountCurrency != marginLeg.NativeMargin * margin.ConversionRate.Value)
            {
                return false;
            }

            if (SameCurrency(marginLeg.NativeCurrency, accountCurrency))
            {
                if (margin.ConversionRate != 1m) return false;
            }

            marginSum = checked(marginSum + marginLeg.MarginAccountCurrency);
        }

        return totalMargin == marginSum;
    }

    private static bool SameCurrency(string left, string right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static SyntheticPreflightFailure Failure(string reason) => new("", reason);

    private static SyntheticPreflightFailure Failure(string epic, string reason) => new(epic, reason);

    private static string? NormalizeSide(string side) =>
        side.Trim().ToUpperInvariant() switch
        {
            "BUY" => "BUY",
            "SELL" => "SELL",
            _ => null,
        };

    private static string NormalizeEpic(string epic) => epic.Trim().ToUpperInvariant();

    private static bool IsValidRoundedSize(decimal quantity, MarketInstrument instrument)
    {
        if (quantity <= 0m) return false;
        if (instrument.MinDealSize is > 0m && quantity < instrument.MinDealSize.Value) return false;
        if (instrument.MaxDealSize is > 0m && quantity > instrument.MaxDealSize.Value) return false;
        return instrument.MinSizeIncrement is not > 0m || quantity % instrument.MinSizeIncrement.Value == 0m;
    }

    private static SyntheticMarginSidePreview? MarginForSide(SyntheticMarginSummary? margin, string side) =>
        margin is null
            ? null
            : side == "BUY"
                ? margin.Buy
                : margin.Sell;

    private static SyntheticMarginLegPreview? FindMarginLeg(
        SyntheticMarginSidePreview margin,
        ExecutableOrderLegPreview leg,
        bool requireExactSizing) =>
        margin.Legs.SingleOrDefault(candidate =>
            string.Equals(candidate.Epic, leg.Epic, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.Side, leg.Side, StringComparison.OrdinalIgnoreCase) &&
            (!requireExactSizing ||
             (candidate.ReferencePrice == leg.ReferencePrice &&
              candidate.Quantity == leg.Quantity &&
              candidate.NativeNotional == leg.Notional)));
}
