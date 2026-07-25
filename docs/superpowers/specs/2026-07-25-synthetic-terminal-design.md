# Full-Screen Synthetic Terminal

## Goal

Build a professional terminal view in the local CAPETF Windows app for one
automatically selected synthetic stock instrument. This is not a dashboard
tile view. It is a full-screen charting workspace similar in intent to an MT5
synthetic instrument screen.

The first delivery focuses on charting and real-time synthetic pricing. Live
Buy/Sell execution is a later step after the synthetic chart and component
prices are correct.

## User Experience

The app gets a dedicated `Terminal` workspace mode. When opened, it uses the
currently loaded stock universe and automatically selects the best available
synthetic basket from the selected block.

The terminal screen is chart-first:

- Main center area: large candlestick chart covering most of the window.
- Top compact bar: synthetic symbol, block, currency status, timeframe, live
  connection status, last tick time.
- Right compact panel: 3 to 4 underlying component stocks with weights, epics,
  bid, offer, last price, and latest tick time.
- Bottom compact strip: synthetic OHLC, MA values, spread estimate, status and
  errors.

No stock-grid rows or mini charts render inside Terminal mode. This avoids the
current laggy dashboard behavior.

## Automatic Synthetic Selection

The app chooses one synthetic basket automatically using the existing
`SyntheticBasketBuilder` logic:

1. Use only Capital.com `SHARES` instruments.
2. Use the selected block, such as `US / USD / Technology` or the current
   fallback block when Capital.com returns blank currency.
3. Fetch approximately three years of historical OHLC data for each candidate.
4. Build candidate synthetic baskets from 3 to 4 stocks with similar chart
   paths and relatively similar individual volatility.
5. Select the highest-similarity basket with valid candles.

The user can later switch to another generated basket, but the first terminal
version opens with a valid basket automatically if one can be built.

Terminal selection must use the last three years as the comparison window.
The ranking must compare normalized chart shape and individual annualized
volatility. A basket with close chart shapes but materially different
volatility must rank below a basket whose components have both close chart
shape and close volatility.

## Charting Library

Use TradingView Lightweight Charts inside the existing local WebView2 host.

Reasons:

- It is designed for financial candlestick charts.
- It supports real-time candlestick updates through incremental series updates.
- It supports moving-average overlays as line series.
- It avoids heavy WPF per-row rendering and keeps chart work inside the browser
  canvas.

The chart HTML remains local and packaged with the desktop app. It does not
call Capital.com directly. WPF sends chart data and updates into WebView2.

## Chart Content

The chart must show:

- synthetic candlesticks using weighted component OHLC
- current in-progress synthetic candle updated by live component ticks
- MA 20
- MA 50
- MA 200 when enough candles exist
- current synthetic last price marker
- compact component ratio legend
- block and currency label

The chart must resize with the window and use most of the available screen.
It must not be constrained to the current small right-side panel.

## Real-Time Data

Historical candles come from Capital.com REST price endpoints.

Live prices come from the existing Capital.com streaming client. For the
selected synthetic instrument, subscribe only to its 3 to 4 component epics.
This keeps well inside the Capital.com streaming limit of 40 instruments.

When a component tick arrives:

1. Update that component bid, offer, last price and tick time.
2. Recalculate synthetic last price from component weights.
3. Recalculate the current synthetic candle.
4. Push an incremental chart update into TradingView Lightweight Charts.

The app must not poll for live updates in Terminal mode. It must update as soon
as Capital.com streaming data arrives. There is no intentional delay or batch
timer in the pricing path. A small UI throttle can be added only if required to
prevent rendering overload, and it must not delay the underlying price state.

## Synthetic Price and Currency

The synthetic price is an analytical weighted basket:

`synthetic price = sum(component price * component weight)`

Synthetic OHLC uses the same weighted formula for open, high, low and close.

If all components have a known matching currency, display that currency. If
Capital.com returns blank currency for the instruments, display
`currency unavailable from Capital.com` and show the selected block name. Do
not mix known different currencies inside one synthetic instrument.

## Buy/Sell Boundary

The first terminal implementation shows disabled or preview-only Buy/Sell
controls. Live execution is deliberately out of scope for this step.

Later, Buy/Sell will create a parent synthetic order preview and split it into
child orders across the 3 to 4 underlying Capital.com epics by weight. That
requires a separate execution spec covering order type, quantity rounding,
partial failures, rejected instruments, margin checks and confirmation.

## Performance Requirements

Terminal mode must avoid rendering the discovery dashboard:

- no thousands of instrument rows
- no per-row mini charts
- no full stats recalculation on every tick
- no broad market subscriptions

Only the selected synthetic basket and its components update in real time.

## Error Handling

If no synthetic basket can be created, the terminal shows a clear empty state:

- not enough valid stocks in the selected block
- historical candles unavailable
- streaming not connected
- currency mismatch
- WebView2 chart unavailable

If one component stops receiving ticks, keep the last valid component price and
show its tick age. Do not silently remove the component from the synthetic
instrument.

## Testing

Tests must cover:

- automatic basket selection from a block
- three-year terminal selection window
- chart-shape similarity and volatility similarity in terminal selection
- chart payload generation including candles and MA lines
- weighted live tick recalculation
- current candle update from a component tick
- known-currency grouping and blank-currency fallback
- no dashboard redraw path used for terminal tick updates

Manual verification must include:

- connect with saved Capital.com API credentials
- open Terminal mode
- build/select a synthetic instrument automatically
- confirm a large full-screen chart appears
- confirm MA lines appear when enough candles exist
- stream component quotes
- confirm synthetic price and current candle update when ticks arrive
