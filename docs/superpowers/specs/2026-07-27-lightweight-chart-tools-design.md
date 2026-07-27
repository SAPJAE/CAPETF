# Lightweight Chart Tools Design

## Goal

Replace the terminal's temporary drawing buttons with a professional, compact drawing workspace on top of TradingView Lightweight Charts 5.2. Add a percentage/price range measurement tool and safe deletion of saved synthetic baskets without changing the Capital.com data feed or synthetic pricing engine.

## Library Decision

Keep TradingView Lightweight Charts 5.2 as the chart engine. Replacing it would require rebuilding the proven live-feed, synthetic-candle, moving-average, viewport, and persistence integrations. Use the MIT-licensed `deepentropy/lightweight-charts-drawing` project as the reference implementation for v5 primitive lifecycle, hit testing, selection, and geometry. Vendor only adapted code needed by CAPETF so the published Windows app remains self-contained and offline-capable.

## Drawing Workspace

The current `X / TL / HL / VL / RAY / RECT` dock is removed. A narrow left toolbar uses familiar icons and hover tooltips for:

- Cursor/select
- Trend line
- Ray
- Horizontal line
- Vertical line
- Fibonacci retracement
- Rectangle
- Freehand brush
- Text annotation
- Price/percentage range measure

The measure tool uses two chart points and displays start price, end price, absolute price change, percentage change, bar count, and elapsed time. Positive and negative measurements use distinct restrained colors.

Selected drawings expose anchor handles and can be moved or reshaped. A compact contextual style bar controls color, width, and solid/dashed/dotted line style. `Delete` removes the selected drawing. `Escape` cancels the active operation and returns to cursor mode.

Top chart controls add undo and redo. Bottom-left controls provide magnet snapping, lock/unlock all drawings, hide/show drawings, and clear drawings. Clearing all drawings requires confirmation.

## Persistence

Drawings remain local to the Windows user and persist per saved or active synthetic basket. The persistence key uses the stable basket symbol plus its sorted component identity, preserving existing behavior. The serialized schema includes tool type, anchors, text, style, visibility, and lock state. Invalid or legacy entries are ignored without blocking chart loading.

## Saved Basket Deletion

A trash icon sits beside the saved-basket selector and is disabled until a saved basket is selected. Clicking it asks for confirmation using the basket name. Confirmation removes the saved definition from local storage and refreshes the selector. If that basket is currently displayed, its chart remains open for analysis until another basket is built or selected.

## Architecture

- `synthetic-terminal.html` owns toolbar layout, keyboard bindings, drawing selection, and communication with the chart.
- A focused local drawing module owns drawing models, coordinate conversion, hit testing, rendering primitives, interaction state, serialization, and undo history.
- `SavedSyntheticBasketStore` owns basket deletion; the WPF window only coordinates confirmation and refreshes the list.
- No network dependency is introduced at runtime.

## Error Handling

Drawing failures are isolated from market data and chart rendering. Unsupported persisted records are skipped. Operations requiring a chart point do nothing outside the plot area. Locked drawings cannot be edited or deleted individually. Basket deletion failures show a concise status message and do not change the selector.

## Testing And Acceptance

- Automated tests cover measurement math, serialization, undo/redo, lock/visibility state, stable persistence identity, and saved-basket deletion.
- Existing synthetic construction, history, bid/ask, and streaming tests remain green.
- Publish the unzipped Windows executable.
- Launch the published app, connect to the Capital.com demo API, build a synthetic basket, draw and edit multiple tools, verify the percentage measurement, reload to verify persistence, and delete a saved basket.
- Capture desktop screenshots confirming the chart, toolbar, measurement label, editable drawing, and non-overlapping layout.

## Deferred

Trading execution, Advanced Charts integration, and the complete 68-tool catalog are outside this change. The drawing boundary remains replaceable if TradingView grants Advanced Charts access later.
