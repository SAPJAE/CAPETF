namespace CAPETF.Desktop;

public static class SyntheticExecutionTicketRevalidation
{
    public static void Validate(SyntheticExecutionTicket frozen, SyntheticPreflightResult current)
    {
        ArgumentNullException.ThrowIfNull(frozen);
        ArgumentNullException.ThrowIfNull(current);
        if (!current.IsReady || current.Ticket is null)
        {
            var reason = string.Join("; ", current.Failures.Select(failure =>
                string.IsNullOrWhiteSpace(failure.Epic) ? failure.Reason : $"{failure.Epic}: {failure.Reason}"));
            throw new InvalidOperationException($"Final execution checks failed. {reason}".Trim());
        }

        var refreshed = current.Ticket;
        if (!string.Equals(frozen.BasketId, refreshed.BasketId, StringComparison.Ordinal) ||
            !string.Equals(frozen.Side, refreshed.Side, StringComparison.OrdinalIgnoreCase) ||
            frozen.BasketQuantity != refreshed.BasketQuantity ||
            !string.Equals(frozen.AccountId, refreshed.AccountId, StringComparison.Ordinal) ||
            frozen.UniverseKind != refreshed.UniverseKind ||
            !string.Equals(frozen.MarginCurrency, refreshed.MarginCurrency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Final execution checks no longer match the confirmed basket.");
        }

        var frozenLegs = frozen.Legs.OrderBy(leg => leg.Epic, StringComparer.OrdinalIgnoreCase).ToArray();
        var refreshedLegs = refreshed.Legs.OrderBy(leg => leg.Epic, StringComparer.OrdinalIgnoreCase).ToArray();
        if (frozenLegs.Length != refreshedLegs.Length)
        {
            throw new InvalidOperationException("Final execution leg count changed after confirmation.");
        }

        for (var index = 0; index < frozenLegs.Length; index++)
        {
            var before = frozenLegs[index];
            var after = refreshedLegs[index];
            if (!string.Equals(before.Epic, after.Epic, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(before.Direction, after.Direction, StringComparison.OrdinalIgnoreCase) ||
                before.Multiplier != after.Multiplier ||
                before.Quantity != after.Quantity ||
                !string.Equals(before.MarginCurrency, after.MarginCurrency, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Final execution leg {before.Epic} changed after confirmation.");
            }
        }
    }
}
