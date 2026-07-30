# Demo Synthetic Basket Execution Design

## Goal

Turn the existing synthetic basket order preview into a complete Capital.com demo-account execution workflow. A user can preflight, submit, confirm, monitor, reconcile, and close a multi-leg synthetic basket while retaining a clear record of every underlying Capital.com position.

This release must never send an order to the Capital.com live API.

## Safety Boundary

- Execution is allowed only when the active session was created against the Capital.com demo API host.
- The API client rejects all position-changing requests when the configured endpoint is not the recognized demo endpoint.
- The interface displays a persistent `DEMO TRADING` indicator whenever execution controls are enabled.
- Duplicate submissions are blocked while a basket execution is active.
- Buy and Sell require a final confirmation dialog containing exact legs, directions, quantities, indicative prices, estimated margin, and total executable notional.
- No stop, limit, or guaranteed-stop parameters are inferred. Initial basket orders are market positions without attached protection unless a future explicit design adds those controls.

## Capital.com Contract

The implementation uses the official Capital.com trading contract:

- `POST /api/v1/positions` submits one underlying position and returns an initial order `dealReference`.
- `GET /api/v1/confirms/{dealReference}` determines whether that order was accepted and exposes permanent deal identifiers in `affectedDeals`.
- `GET /api/v1/positions` reconciles confirmed legs with the active account and supplies current position state and unrealized profit or loss.
- `DELETE /api/v1/positions/{dealId}` closes one confirmed position. Its response is also confirmed before that leg is considered closed.
- `GET /api/v1/accounts` refreshes available account funds used by preflight and post-execution summaries.

An HTTP success from `POST /positions` is an acknowledgment only. A leg is not `Open` until its confirmation is accepted and its permanent deal ID is known.

## Preflight

Preflight runs immediately before the confirmation dialog and refreshes all data rather than trusting stale chart state. Every leg must pass:

1. The connection is an authenticated Capital.com demo session for the active account.
2. The basket has three or four distinct components and a positive requested notional.
3. Current market details are available for every epic.
4. `marketStatus` is `TRADEABLE`; closed, suspended, obsolete, close-only, or otherwise unavailable instruments fail preflight.
5. Bid and offer are finite, positive, and fresh enough for execution.
6. The effective direction correctly accounts for negative synthetic multipliers.
7. Quantity satisfies Capital.com's minimum deal size and size increment after executable rounding.
8. Margin factors and any required account-currency conversion are available.
9. Total estimated margin fits within current available account funds.
10. No identical basket execution is already submitting.

The chart ticket shows a compact readiness state: `Ready`, `Market closed`, `Untradable`, `Stale quote`, `Invalid size`, `Insufficient margin`, or `Connection required`. A failed preflight disables placement and lists the affected legs and reasons.

## Execution

The recommended sequential model is used:

1. Freeze an immutable execution snapshot containing the basket ID, side, requested notional, formula, prices, quantities, and preflight time.
2. Submit the first leg through `POST /positions`.
3. Poll its confirmation for a bounded period until accepted, rejected, or timed out.
4. Record the initial reference, permanent deal ID, confirmation status, fill level, and timestamps.
5. Continue to the next leg only after the previous leg is confirmed open.
6. Refresh open positions and account funds when execution finishes or stops.

The app never assumes multi-leg atomicity. If a later leg fails, already opened demo positions remain open as requested. The basket becomes `Needs attention`, subsequent legs are not sent, and the interface identifies both the open and failed legs. No automatic rollback occurs.

## Basket States

Each execution record uses explicit states:

- `Preflighting`
- `Ready`
- `Submitting`
- `Partially open`
- `Open`
- `Needs attention`
- `Closing`
- `Partially closed`
- `Closed`
- `Rejected`

Each leg independently records `Pending`, `Submitted`, `Confirming`, `Open`, `Rejected`, `Unknown`, `Closing`, or `Closed`. Unknown and timed-out submissions are reconciled before the app permits a retry, preventing duplicate orders.

## Position Management

An Orders and Positions workspace persists demo synthetic executions locally and reconciles them with Capital.com after connection and on manual refresh. It displays:

- Synthetic basket name, direction, notional, state, and creation time.
- Every underlying epic, effective side, requested and filled size, fill level, current bid/offer, deal reference, deal ID, and status.
- Per-leg and basket unrealized profit or loss in account currency when Capital.com provides sufficient data.
- Available funds and margin snapshots before and after execution.
- Rejection, timeout, and reconciliation messages without hiding previous successful details.

`Close Basket` is available for tracked open legs. It shows a separate confirmation dialog, submits one close request per open deal, confirms each response, and preserves partial-close state if any close fails. The user can also refresh/reconcile without placing or closing anything.

## Interface

- Rename the chart controls to `Place Buy Basket` and `Place Sell Basket`.
- Show a persistent `DEMO TRADING` badge and current account identity near the connection state.
- Give the right rail a movable separator and independent vertical scrollbar.
- Keep the readiness and margin summary sticky while Formula, Preflight, Execution, and Position Details are independently collapsible.
- Show a bottom-right progress indicator with the current leg and operation, such as `Submitting 2 of 3` or `Confirming EBC`.
- Prevent chart and live-price updates from replacing an in-progress execution snapshot.
- Keep the full response/error history accessible without allowing long text to overlap controls.

## Persistence And Recovery

Execution records are written atomically to a versioned local JSON store after every state transition. Credentials and session tokens are not written into this file. On startup or reconnect:

1. Load local records.
2. Fetch current Capital.com demo positions.
3. Match tracked deal IDs and update open/closed/P&L state.
4. Mark unmatched unresolved legs `Unknown` and require reconciliation before retry.

This makes a basket recoverable after an app crash or restart without treating local state as the source of truth.

## Error Handling

- Authentication expiry triggers one controlled session refresh, then fails visibly.
- Rate-limit responses use bounded backoff without resubmitting a position whose acknowledgment may have been received.
- A missing or malformed deal reference stops execution.
- Confirmation timeout produces `Unknown`, not `Rejected`, and triggers position reconciliation.
- Network failure after submission never causes a blind retry.
- UI cancellation stops unsent legs but cannot cancel a leg already accepted by Capital.com.
- All user-facing errors include the affected epic and operation while secrets and tokens remain excluded.

## Testing And Demo Verification

Automated tests cover:

- Hard blocking of live-host position mutations.
- All preflight rejection states and stale quote handling.
- Effective side and rounded quantity for positive and negative legs.
- Sequential submission and confirmation ordering.
- Accepted, rejected, malformed, timed-out, and network-interrupted confirmations.
- Partial execution without automatic rollback.
- Duplicate-click and ambiguous-retry prevention.
- Persistence and restart reconciliation.
- Closing complete and partially closed baskets.
- Scrollable/collapsible order details and unambiguous demo labels.

After the automated suite and release build pass, the published Windows app is tested with a small three-leg basket on the user's Capital.com demo account. The verification must show accepted confirmations, permanent deal IDs, matching entries from `GET /positions`, visible current P&L, successful restart reconciliation, and a clean usable order panel. The demo positions remain open for the user to inspect unless the user explicitly closes them.
