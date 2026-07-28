# Synthetic Basket Margin Preview Design

## Goal

Show the total estimated margin required to buy and sell the current synthetic basket, together with the active Capital.com account's available funds and estimated funds remaining after either transaction.

## Data Sources

- `GET /markets/{epic}` supplies each leg's current bid, offer, lot size, minimum deal size, size increment, `marginFactor`, and `marginFactorUnit`.
- `GET /accounts` supplies the active account's currency and current `balance.available` value.
- Capital.com prices supply any required conversion between the basket currency and the account currency.

Capital.com does not expose a non-trading order-margin preview in its official Postman collection. The terminal therefore labels the result as an estimated margin derived from Capital.com's current market metadata.

## Calculation

The existing executable order sizing remains authoritative for leg quantities. It applies minimum deal sizes and size increments before margin is calculated.

For each BUY or SELL basket preview:

1. Resolve the effective side of every leg. A negative formula multiplier reverses the requested basket side.
2. Use the offer for a buying leg and the bid for a selling leg.
3. Calculate the executable leg notional from quantity, price, and lot size.
4. Apply the instrument margin factor according to its declared unit. Percentage factors divide by 100. Unsupported units make the margin unavailable rather than triggering a guessed result.
5. Convert each leg margin to the active account currency when necessary.
6. Sum converted leg margins separately for the BUY and SELL basket previews.
7. Calculate remaining available funds as account available funds minus required margin.

The displayed total follows the executable rounded quantities, so it can differ slightly from a simple basket-notional percentage.

## Interface

The order preview rail displays a compact summary that updates when the basket, notional, or current prices change:

- BUY margin
- SELL margin
- Available
- After Buy
- After Sell

All values use the active Capital.com account currency. The expanded order preview includes each leg's effective side, quantity, execution price, notional, and margin contribution.

Negative remaining funds are shown in the existing warning color. The Buy and Sell controls remain previews only and do not submit live orders.

## Refresh And Failure Behavior

- Account availability is refreshed from Capital.com when the user requests a preview and is cached briefly to avoid API-rate pressure.
- Market details already loaded for basket components provide margin metadata; missing metadata is fetched before the preview completes.
- Current stream bid/ask values are used when available.
- The UI shows a loading state while metadata, account funds, or conversion prices are being fetched.
- If any required leg margin or currency conversion cannot be determined, that side displays `Unavailable` with a concise reason. No zero or fabricated fallback is shown.
- API or session failures leave the last successful account figure visible but clearly marked stale.

## Testing

Automated tests cover:

- Percentage margin factors.
- Different BUY and SELL totals from bid/ask prices.
- Negative synthetic legs reversing execution side.
- Minimum deal size and size-increment rounding before margin calculation.
- Lot-size application.
- Currency conversion into the active account currency.
- Missing or unsupported margin metadata.
- Account availability and after-trade calculations.
- Serialization and browser rendering of the summary and leg details.

The published Windows package is then exercised against the Capital.com demo API with a multi-leg basket and visually checked in the terminal.
