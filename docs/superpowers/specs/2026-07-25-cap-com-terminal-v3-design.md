# cap.com Terminal V3 Design

## Purpose

Rebuild the Windows terminal chart experience so it behaves like an analysis tool, not a static preview. The existing Capital.com API, stock universe loading, synthetic basket selection, and live update logic stay in place. The current WPF Canvas chart is replaced because it does not support timeline navigation, zooming, crosshair analysis, or a professional trading layout.

## Scope

V3 focuses on charting and usability only.

- Full-screen-first synthetic instrument chart.
- Interactive timeline with mouse zoom, pan, crosshair, price scale, and fit/reset.
- Candle modes: normal candles and Heikin Ashi.
- Moving averages: 20, 50, and 200 period overlays.
- Timeframe selector using existing available candle data.
- Live Capital.com streaming updates applied to the visible synthetic candle.
- Compact top toolbar with seed symbol, region/currency block, component ratios, chart mode, timeframe, and stream controls.
- Existing Nike sample must open directly as a usable synthetic chart.

Order placement is out of scope for this pass. Buy/sell execution will be added only after the chart is reliable.

## Recommended Architecture

Use WPF as the native shell and WebView2 as the chart host.

- WPF owns credentials, data loading, Capital.com API calls, stream lifecycle, and synthetic basket state.
- WebView2 hosts an HTML chart page using the existing local `lightweight-charts.standalone.production.js` asset.
- WPF sends chart payloads to JavaScript as JSON.
- JavaScript renders candles, Heikin Ashi, moving averages, crosshair, and timeline controls.
- Live updates flow from Capital.com streaming into WPF, then into the WebView chart through a small update message.

This avoids the StockSharp/DevExpress runtime failure already seen in this environment and avoids extending the static Canvas approach.

## Components

### Terminal Window

`CapComTerminalWindow` becomes a compact control shell around a large chart:

- Top bar: title, connection state, seed symbol, block selector, build button, stream button.
- Second bar: timeframe, candle type, MA toggles, fit/reset controls, current synthetic price.
- Main area: WebView2 chart occupying most of the window.
- Side drawer or compact panel: synthetic components with ratio, symbol, price, and stream status.

### Chart Host

Add a local chart page under `desktop/CAPETF.Desktop/Assets/`:

- Loads Lightweight Charts from the bundled asset.
- Exposes JavaScript functions for full chart replacement, live candle updates, chart mode switches, MA visibility, and resize.
- Computes Heikin Ashi candles client-side from OHLC payloads.
- Computes MA series client-side from the selected candle mode.

### Data Flow

1. User chooses or seeds a synthetic basket.
2. WPF builds the synthetic candle series from existing cached Capital.com data.
3. WPF sends the full payload to WebView2.
4. WebView2 renders the interactive chart.
5. If streaming is enabled, WPF receives component quote updates.
6. WPF recalculates the current synthetic price/candle and sends an incremental update to WebView2.

## Error Handling

- If WebView2 is unavailable, show a clear missing-runtime message instead of a blank panel.
- If no candles exist for a selected timeframe, show an in-chart message with the reason.
- If streaming fails, leave historical chart usable and show connection failure in the toolbar.
- If a component has no live quote, keep its last historical value and mark it stale.

## Testing And Verification

Automated checks:

- Existing synthetic basket tests must continue passing.
- Add coverage for chart payload generation where practical.
- Build the WPF app in Release.

Manual verification:

- Launch packaged app.
- Open Nike sample.
- Confirm chart is not blank.
- Confirm mouse wheel zoom, drag pan, crosshair, fit/reset, timeframe switch, and Heikin Ashi mode.
- Confirm stream button does not freeze the UI.
- Take a screenshot after verification.

## Acceptance Criteria

- The app opens as `cap.com Terminal`.
- Nike sample displays a full-screen interactive synthetic candle chart.
- Chart supports zoom, pan, timeline, crosshair, price scale, fit/reset, and Heikin Ashi.
- Current static Canvas chart is no longer the primary charting surface.
- Existing Capital.com stock universe and synthetic basket logic still works.
- Release ZIP is rebuilt and ready for use.
