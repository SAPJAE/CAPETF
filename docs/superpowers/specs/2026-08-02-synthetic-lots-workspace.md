# Synthetic Lots And Trading Workspace Design

## Purpose

Make synthetic trading predictable: one displayed basket formula is one synthetic lot, controls remain stable during background work, margin is visible before submission, and every host/API action produces an understandable activity event.

## Lot Contract

- The chart price is the price of one complete synthetic basket formula.
- The order input is `Synthetic lots`, defaults to `1`, accepts positive whole numbers, and increments by one.
- Entering `N` multiplies every formula leg quantity by `N`.
- The preview, margin calculation, confirmation, execution record, chart overlay, and P/L all use the same lot count.
- Capital.com minimum deal sizes and increments are validated before confirmation. The app never silently changes the requested synthetic lot count; an incompatible formula is rejected with the affected leg and rule shown.

## Stable Ticket

- Background quote, margin, account, and universe requests do not disable or recreate the ticket.
- The quantity retains focus and its typed value while data publications update individual text nodes.
- Buy and Sell remain clickable enough to explain why an action cannot proceed. Submission itself is guarded against duplicate clicks.
- Changing quantity debounces margin calculation and marks values as updating without blanking the previous valid values.

## Workspace Ownership

- The right rail contains only the selected synthetic instrument: formula, constituent legs, bid/ask, lot input, margin preview, Buy/Sell, selected position P/L, SL/TP, and close action.
- The bottom dock contains account-wide Positions, Pending Orders, Active Baskets, History, and Activity Log.
- `Audit` is renamed `Activity Log`. Unrelated positions and execution streams are removed from the right rail.

## Activity Log

- Events have timestamp, severity (`success`, `info`, `warning`, `error`), operation, summary, and optional technical detail.
- Connection, universe loading, history loading, quote/stream state, margin preview, preflight, execution, partial execution, close/reconciliation, and API failures are logged.
- Logs persist locally between application starts and support Clear and Export.

## Margin Preview

- Immediately below Buy/Sell, show synthetic lots, basket price, estimated notional, Buy margin, Sell margin, available margin, and remaining margin for each side.
- A quantity change refreshes the preview without blocking typing.
- Insufficient margin is visible before the user opens confirmation.

## Chart Risk Interaction

- An active selected basket displays Entry, planned SL, and planned TP lines.
- A small `+` risk control beside the entry creates missing SL and TP lines at sensible offsets.
- Planned SL and TP are draggable on the price scale/chart. Dragging updates the local plan preview continuously and persists it on drop.
- The direction rule is validated: Buy requires `SL < Entry < TP`; Sell requires `TP < Entry < SL`.
- These are visual synthetic-basket plans until explicit broker-side synthetic protection is implemented; the UI states that clearly.

## Progressive Universe

- Connect publishes a local cached universe immediately.
- Capital.com discovery continues in background batches and merges instruments by epic without replacing the current selection.
- Saved baskets, open positions, and the selected region/currency are prioritized.
- Progress is shown in the Activity Log and a compact background progress indicator.
- Completed merged universes are persisted locally for the next startup.

