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

        if (!input.IsDemoSession) globalFailures.Add(Failure("Demo trading session is required."));
        if (string.IsNullOrWhiteSpace(input.BasketId)) globalFailures.Add(Failure("Basket ID is required."));
        if (input.RequestedNotional <= 0m) globalFailures.Add(Failure("Requested notional must be positive."));
        if (side is null) globalFailures.Add(Failure("Side must be BUY or SELL."));
        if (input.Basket.Components.Count is < 3 or > 4)
        {
            globalFailures.Add(Failure("Synthetic baskets must contain 3 or 4 components."));
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
            if (component.Instrument.LastTickAt is null ||
                input.NowUtc - component.Instrument.LastTickAt.Value > MaximumQuoteAge)
            {
                legFailures.Add(Failure(epic, "Quote is older than five minutes."));
            }
        }

        ExecutableOrderPreview? executable = null;
        if (input.RequestedNotional > 0m && input.Basket.Components.Count > 0 &&
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
            catch (InvalidOperationException)
            {
                globalFailures.Add(Failure("Executable order sizing is unavailable."));
            }
        }

        var margin = side is null ? null : MarginForSide(input.Margin, side);
        if (margin is null || !margin.IsAvailable || margin.TotalMargin is null || input.Margin?.IsAccountStale == true)
        {
            globalFailures.Add(Failure("Margin preview is unavailable."));
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
                    if (FindMarginLeg(margin, leg) is null)
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
                var marginLeg = FindMarginLeg(margin, leg)!;
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
            Array.AsReadOnly(legs));

        return new SyntheticPreflightResult(true, ticket, Array.Empty<SyntheticPreflightFailure>());
    }

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
        ExecutableOrderLegPreview leg) =>
        margin.Legs.SingleOrDefault(candidate =>
            string.Equals(candidate.Epic, leg.Epic, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.Side, leg.Side, StringComparison.OrdinalIgnoreCase));
}
