# cap.com Terminal V4 Design

## Purpose

V4 turns the existing synthetic-chart window into a dependable analysis workspace. It keeps the WPF, WebView2, Lightweight Charts, and Capital.com API architecture while correcting quote presentation, history depth, synthetic index construction, loading feedback, panel ergonomics, and ETF coverage.

The terminal remains an analysis and staging tool in this release. It does not place live orders.

## User Experience

The main chart remains the dominant surface. The compact toolbar contains the universe selector, region/currency group, seed symbol, strategy, basket controls, timeframe, candle mode, moving averages, and chart navigation.

The chart header shows:

- synthetic symbol;
- selected universe and group;
- chart currency;
- synthetic bid;
- synthetic ask;
- component count and selection summary.

The terminal does not display a synthetic last price. Bid and ask price lines are the only quote lines on the chart.

The `Legs / Formula` rail is separated from the chart by a draggable vertical splitter. The chosen width persists locally. The existing show/hide control still collapses the rail completely.

A compact bottom-right progress panel appears for operations that can take noticeable time: connection, universe loading, candidate evaluation, component history loading, market-detail loading, basket construction, and stream startup. It displays the active operation and uses determinate progress when a total is known; otherwise it uses an indeterminate bar. Controls that would start the same operation are disabled until it completes.

## Universe And Grouping

Add a universe selector with `Stocks` and `ETFs`. Stocks remain the default.

Each universe is grouped primarily by region and currency. The terminal uses the existing region/currency classification and retains sector or instrument-type detail as the third grouping level where available. ETF groups must not be mixed with stocks during candidate selection.

The stock universe continues to load from the existing dashboard chunks and Capital.com fallback. The ETF universe loads from the existing encrypted ETF dashboard dataset and can fall back to Capital.com market search. Only instruments that are available for opening under the existing tradeability rules are eligible. A closed market remains eligible; a close-only or obsolete instrument does not.

Changing universe rebuilds the group and symbol lists without reconnecting. Building a basket uses only instruments from the selected universe and group.

## Synthetic Index Construction

V4 separates chart normalization from executable leg sizing.

### Display Index

The displayed synthetic chart is a performance index. For the selected components, the builder finds the earliest candle present for every component at the selected timeframe. Each component receives an equal one-third or one-quarter notional contribution at that shared candle. Their contributions sum to an initial synthetic value of `100` at that first common candle.

Later candles and live bid/ask quotes use those fixed display multipliers. The latest chart value is therefore the basket's cumulative performance since the first shared candle; it is not forcibly reset to `100` today.

The chart uses the strict timestamp intersection already required for synthetic candles. Missing data from one leg does not create a partial synthetic candle.

### Executable Preview

Order-preview quantities are calculated separately from the display multipliers. Each leg receives equal current notional, adjusted to Capital.com's `minDealSize` and `minSizeIncrement`. The preview shows the rounded executable quantity and resulting notional imbalance. Display-index multipliers must never be presented as order quantities.

Synthetic bid and ask are calculated from current component quotes and the fixed display multipliers. A missing or zero component quote makes the corresponding synthetic side unavailable rather than displaying zero.

## History Loading

Basket discovery remains fast by using cached history for candidate comparison. After the three or four components are selected, the terminal requests full Capital.com history for those components only and rebuilds the same basket from the returned series.

Timeframe sources are:

- `Weekly`: Capital.com `WEEK` candles;
- `Daily`: Capital.com `DAY` candles;
- `4H`: Capital.com `HOUR_4` candles;
- `2H` and `6H`: Capital.com `HOUR` candles aggregated locally into two-hour and six-hour candles.

Requests page backward using Capital.com's maximum page size until the API supplies no older data, the instrument's history begins, or the API rejects an older window. Weekly and daily should target at least three years and continue to all available history. Intraday should load the maximum history Capital.com makes available; V4 does not invent candles or promise three years where the API does not provide it.

The chart status shows the actual shared date range and candle count after intersection. Switching timeframe preserves the selected components and reloads their full history rather than rerunning basket selection.

## Quotes And Streaming

Connection loads the selected universe automatically. Building or restoring a basket starts streaming automatically after market details and history are available.

Capital.com market details supply current bid, offer, lot size, minimum deal size, and size increment. The WebSocket stream updates the visible synthetic bid and ask and the current synthetic candle when a matching component quote arrives.

If the market is closed, the most recent non-zero Capital.com bid and offer remain visible and are marked stale with their timestamp. Zero values are treated as unavailable. A stream failure leaves the historical chart and last valid quote visible and reports the failure without freezing the interface.

## Chart Tools

The bundled Lightweight Charts library supplies zoom, pan, crosshair, timeline, price scale, and series rendering. It does not contain TradingView's licensed Advanced Charts drawing toolbar, so V4 extends the existing local primitive system.

V4 provides:

- crosshair/select;
- trend line;
- horizontal line;
- vertical line;
- extended ray;
- rectangle;
- clear drawings.

Drawings use chart coordinates, survive live candle updates and timeframe redraws during the session, and are stored locally per saved synthetic symbol. This is the supported drawing scope for V4. A future move to TradingView Advanced Charts requires its separately licensed package and is outside this release.

## Component Boundaries

- `CapComTerminalWindow`: coordinates connection, universe selection, operation progress, basket lifecycle, history refresh, and streaming.
- `DashboardStockChunkLoader` or a focused companion loader: reads cached stock and ETF dashboard datasets into a common instrument/history result.
- `CapitalApiClient`: retrieves paged historical candles and current market details without UI responsibilities.
- `SyntheticBasketBuilder`: finds the first shared baseline, creates fixed equal-notional display multipliers, and builds intersected synthetic candles.
- `SyntheticOrderSizing`: converts equal current notional into valid Capital.com order-preview quantities.
- `SyntheticTerminalChartPayload`: emits chart, quote, currency, component, and selection metadata without a last-price display contract.
- `synthetic-terminal.html`: renders the chart, drawing primitives, quote lines, progress-independent chart state, and resizable component rail.

## Error Handling

- No eligible instruments: keep the current chart visible and explain which universe/group produced no candidates.
- Insufficient common history: identify the component and timeframe that prevented intersection.
- Capital.com history limit: render all returned shared candles and show the true range.
- Missing bid or ask: show `n/a`; never substitute zero or a historical close.
- Connection or stream failure: preserve loaded data, restore enabled controls, and show the operation error in both status and progress panel.
- ETF cache unavailable: attempt the Capital.com fallback and state which source was used.

## Testing And Verification

Automated tests must cover:

- first shared candle is normalized to `100` while the latest candle is allowed to differ;
- strict intersection across all selected components;
- synthetic bid and ask calculations and missing-zero quote behavior;
- executable quantities respect minimum deal size and size increment;
- daily, weekly, 2H, 4H, and 6H request/aggregation mapping;
- timeframe changes preserve component epics;
- ETF recognition, cache loading, and separation from stock candidates;
- chart HTML exposes the splitter, loading state hooks, drawing tools, bid/ask-only metadata, and no last-price line.

Manual release verification must:

1. Connect with saved Capital.com credentials and confirm the default stock universe loads automatically.
2. Build a three-stock basket and confirm full history, non-zero bid/ask, currency, zoom, pan, crosshair, drawing tools, and splitter behavior.
3. Switch through Weekly, Daily, 6H, 4H, and 2H and confirm the same component epics remain selected and the reported range matches loaded data.
4. Switch to ETFs, select a currency group, build a synthetic ETF basket, and confirm no stock components are included.
5. Confirm the loading panel appears during slow work and prevents duplicate operation starts.
6. Publish an unpacked Windows executable and launch it from outside the build directory.

## Acceptance Criteria

- Only bid and ask are shown as synthetic quote values and chart quote lines.
- Bid and ask use current Capital.com data when available and never display false zeroes.
- The component rail is resizable and collapsible.
- Slow operations provide visible bottom-right progress and cannot be triggered repeatedly while running.
- Daily and weekly load all available shared history; intraday loads all history available from Capital.com for the selected components.
- The displayed index starts at `100` on the first shared candle instead of ending at `100` today.
- Chart currency and actual shared history range are visible.
- Stocks and ETFs can each produce baskets within their own region/currency groups.
- The app includes the defined Lightweight Charts drawing primitives and preserves drawings during a session.
- The test suite passes, the Release build succeeds, and the unpacked executable launches successfully.
