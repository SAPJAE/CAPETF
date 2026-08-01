# Crypto Synthetic Universe Design

## Goal

Add Crypto as a first-class cap.com Terminal universe and validate a manual synthetic instrument whose one basket unit is:

```text
9 x ETH/USD + 0.2 x BTC/USD
```

The implementation must preserve the terminal's existing broker-authoritative pricing, history intersection, streaming, margin preflight, confirmation, persistence, charting, risk-plan, and demo execution safeguards.

## External Contracts

Capital.com remains authoritative. The implementation follows the bundled Capital.com Postman reference and official API contracts:

- `GET /markets` and `GET /markets/{epic}` for discovery, status, currency, streaming availability, and dealing rules.
- `GET /marketnavigation` and `GET /marketnavigation/{nodeId}` where needed to discover the complete crypto category.
- `GET /prices/{epic}` for historical candles.
- `OHLCMarketData.subscribe` for live chart bars and `marketData.subscribe` where quote streaming is required.
- Existing position, confirmation, account, margin, and working-order endpoints for demo execution and reconciliation.

Crypto instruments are identified by normalized Capital instrument type `CRYPTOCURRENCIES`. Instruments marked close-only, view-only, reduce-only, suspended, obsolete, or otherwise non-openable are excluded. `CLOSED` is retained as a temporary market state rather than treated as obsolete.

## Universe And Grouping

Add `TerminalUniverseKind.Crypto` and a `Crypto` selector item beside Stocks and ETFs. Crypto loads after the authenticated demo connection and is cached in memory independently from the other universes.

Only accepted crypto instruments may enter this universe. Baskets are crypto-only and same quote currency. Groups use:

```text
Crypto / USD
Crypto / EUR
Crypto / GBP
```

The quote currency comes from Capital market details, not symbol suffix guessing. The seed dropdown supports names, symbols, and epics, including Bitcoin/BTC and Ethereum/ETH aliases returned by Capital.

## Discovery

The loader first calls `GET /markets` without a search term and filters `CRYPTOCURRENCIES`. If the account or endpoint returns an incomplete catalogue, it traverses market-navigation nodes whose names identify crypto and merges their markets by epic. It then enriches accepted markets in bounded batches through market details.

Discovery must not rely on a single search term such as `crypto`, `BTC`, or `ETH`. Duplicate epics are collapsed. Status and dealing-rule enrichment failures leave an instrument unavailable rather than silently executable.

## Manual Synthetic Formula

Add a `Manual formula` strategy. The manual editor contains two to four component rows with:

- instrument selector restricted to the selected crypto currency group;
- signed multiplier;
- live reference price, currency, minimum deal size, and size step;
- add/remove row controls.

For the validation preset, the editor resolves the actual Capital epics for ETH/USD and BTC/USD and creates:

```text
SYN-CRYPTO-ETHBTC-01 = 9 x ETH/USD + 0.2 x BTC/USD
```

The multiplier is the quantity per one basket unit. The existing Basket notional field becomes Basket quantity for manual formulas. A basket quantity of `q` submits `9q` ETH and `0.2q` BTC. Every derived leg is rounded only according to Capital's minimum deal size and size step. If exact ratio-preserving execution is impossible, preflight blocks the order and explains which leg violates its dealing rules; it never silently changes the ratio.

## Pricing And Candles

Synthetic Bid and Ask preserve signed execution semantics:

```text
Bid = sum(multiplier >= 0 ? multiplier * leg bid : multiplier * leg offer)
Ask = sum(multiplier >= 0 ? multiplier * leg offer : multiplier * leg bid)
```

Historical synthetic OHLC uses the existing timestamp intersection method. The chart starts only when both ETH and BTC have candles for the selected resolution. Daily and weekly history load as far back as the shared Capital history allows. Intraday resolutions use Capital-supported bars and ongoing live bars.

The current candle is updated from streaming data. Weekend candles are not specially synthesized: they appear only when Capital supplies crypto data. Stale or missing component quotes make the aggregate non-executable and visibly stale.

## Execution And Risk

The existing two-stage demo flow remains unchanged:

1. Resolve current market details and dealing rules for every leg.
2. Calculate exact leg sizes for the chosen basket quantity.
3. Load live Bid/Ask and Capital margin requirements.
4. Show total BUY/SELL margin, available margin, and post-trade availability.
5. Require the existing explicit confirmation before any demo order is submitted.
6. Submit each component separately and confirm every Capital deal reference.
7. Persist the aggregate execution only from confirmed broker results.

The test trade uses the smallest ratio-preserving basket quantity that is valid for both ETH and BTC and fits available demo margin. If no such quantity exists, the system must stop at preflight and report the exact reason; it must not place a partial or unbalanced basket.

Synthetic Entry, running P/L, broker SL/TP, PLAN SL/TP, the trade dock, pending orders, history, and close-basket workflow reuse the existing trusted execution ledger. A partially executed basket enters `Needs attention`; automatic compensation or closure is not introduced.

## User Experience

Crypto uses the current professional terminal workspace. Selecting Crypto changes only the universe-specific group and seed data. Manual-formula controls appear in the existing top workflow and component rail, without adding a new page or decorative panel.

The saved-basket list identifies the asset class and formula. Restoring the ETH/BTC basket reloads its full chart and live subscriptions. The status bar shows loading, history, streaming, preflight, and execution progress.

## Testing And Validation

Automated tests must cover:

- crypto instrument recognition and strict isolation from Stocks/ETFs;
- full-universe discovery, duplicate collapse, and non-openable exclusion;
- quote-currency grouping from market details;
- ETH/BTC alias and epic resolution;
- exact `9` and `0.2` manual multipliers;
- ratio-preserving basket-quantity sizing against Capital dealing rules;
- signed synthetic Bid/Ask and OHLC intersection;
- stale/missing quote blocking;
- margin-safe demo preflight and insufficient-margin rejection;
- saved/restored manual crypto baskets;
- streaming subscriptions and current-bar updates;
- execution confirmation, reconciliation, and existing safeguards.

Release validation uses the saved Capital.com demo credentials. It loads Crypto, constructs the ETH/BTC preset, checks all supported timeframes, verifies live Bid/Ask and an updating current candle, checks margin, and submits one smallest valid demo basket only after the existing confirmation. The resulting component positions, synthetic entry, running P/L, and dock rows must match Capital's broker snapshot. No live-account trade is permitted.

## Non-Goals

- Mixing crypto with stocks or ETFs.
- Inferring currency from ticker suffixes when Capital details are available.
- Inventing weekend prices or candles.
- Bypassing Capital market status, minimum size, size step, margin, or confirmation rules.
- Automatic live trading, unattended strategy execution, or live-account validation.
