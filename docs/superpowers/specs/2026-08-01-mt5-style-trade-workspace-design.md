# MT5-Style Synthetic Trade Workspace

## Objective

Turn the existing cap.com Terminal chart into a professional trading workspace that keeps the synthetic chart dominant while making open positions, pending orders, basket state, margin, and running P/L continuously visible. The design follows the supplied MT5 reference without copying its visual styling.

## Layout

The terminal keeps its compact top toolbar and full chart. The right components rail remains resizable and collapsible, but defaults to a compact formula and order-control view.

A new resizable bottom trade dock spans the chart workspace. It opens to a useful but restrained height and can be minimized to a single status strip. Its tabs are:

- **Positions:** Capital.com open positions, including each underlying leg.
- **Pending Orders:** Capital.com working orders.
- **Synthetic Baskets:** persisted synthetic executions and their aggregate state.
- **History:** locally persisted synthetic execution events and completed baskets.

The dock uses a dense table rather than cards. Columns include symbol or basket, side, quantity, entry, current Bid/Ask, broker SL, broker TP, synthetic SL, synthetic TP, margin, running P/L, state, and available actions. Selecting a basket or one of its legs restores and focuses the matching synthetic chart. The selected row remains visually linked to the chart.

## Chart Information

The chart displays labelled price lines for:

- Synthetic Bid and Ask.
- Synthetic execution Entry.
- Broker stop-loss and take-profit projections when Capital.com returns actual leg-level levels.
- User-defined synthetic SL and TP planning levels.

Broker levels and synthetic planning levels must use different labels and line styles. Synthetic levels are explicitly marked `PLAN SL` and `PLAN TP`; they must never imply that broker protection exists.

A compact overlay in the chart's upper-left area shows the active basket name, currency, direction, leg count, live aggregate P/L, estimated margin, and concise formula. It must not cover a large candle area and can be collapsed.

## Synthetic Risk Planning

For this phase, synthetic SL and TP are visual planning levels with manual-close controls. Users can enter levels from the selected synthetic basket row or its compact chart controls. Valid levels draw immediately and persist with the basket locally.

The app does not automatically close positions when a planning level is touched. Capital.com has no native single synthetic order, and an app-managed guard would stop protecting the basket when the desktop app is closed or disconnected. Automatic basket protection requires a separate always-on execution design.

## Data And State

Capital.com remains authoritative for account equity, funds, available margin, open positions, working orders, quotes, broker SL/TP, and running P/L. The app refreshes broker state on the existing short polling interval and applies streaming quotes to the active basket.

The local synthetic execution ledger remains authoritative for basket identity, formula multipliers, component membership, and execution history. Synthetic planning levels and dock preferences are persisted locally using a versioned store. Browser messages contain only action identifiers and validated planning values; formula and broker mutation data remain host-owned.

The aggregate synthetic entry and risk lines are calculated from the exact persisted multipliers and the matching Capital positions. A line is omitted when the required legs cannot be matched unambiguously.

## Interaction

- Clicking a position leg selects its parent basket and highlights the leg.
- Clicking a synthetic basket restores its exact historical formula and chart.
- The bottom splitter resizes the dock; a minimize control reduces it to the account and P/L strip.
- Existing Buy, Sell, Close Basket, preflight, and confirmation safeguards remain unchanged.
- Manual Close Basket remains a confirmed Capital.com mutation.
- Editing synthetic planning levels is local and does not transmit an order.

## Failure Handling

Disconnected or stale broker data remains visible but is labelled stale. Missing broker SL/TP displays `n/a`. Pending-order failures remain isolated from position rendering. If a persisted basket cannot be matched to current broker positions, the dock shows the basket but the chart labels its execution lines unavailable.

The dock must remain usable when there are thousands of universe instruments because it renders only broker positions, working orders, and persisted baskets. Updates should patch rows without rebuilding the chart.

## Testing And Verification

Tests cover:

- Broker positions, working orders, account values, and broker SL/TP mapping.
- Synthetic Entry, PLAN SL, and PLAN TP calculations from trusted multipliers.
- Persistence and validation of synthetic planning levels.
- Browser request allow-listing and prevention of browser-supplied formula mutation.
- Dock table rendering, tab switching, row selection, minimization, and splitter behavior.
- Honest `n/a` and stale states.
- Regression coverage for existing execution confirmation and close safeguards.

The published Windows executable is visually verified at desktop resolution with a restored open basket, visible Entry and planning lines, populated position rows, live P/L, and no accidental order mutation.

## Out Of Scope

- Automatic closing when synthetic SL or TP is touched.
- Claiming synthetic planning levels are native Capital.com protection.
- Replacing Lightweight Charts with the licensed TradingView Advanced Charts library.
- Changing the existing basket selection and execution formulas.
