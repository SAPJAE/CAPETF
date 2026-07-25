# Nike Seeded Synthetic Design

## Goal

When the terminal search box contains a seed such as `Nike` or `NKE`, the synthetic builder should construct a focused three-stock basket anchored on that seed instead of building a generic block-level basket.

## Behavior

- Load the cached Capital.com stock chunks as before.
- Match the seed by exact epic, exact symbol, or name contains the search text.
- Restrict peers to the same currency, and prefer the same region/block so synthetic pricing is coherent.
- Rank peers by volatility similarity, chart-path similarity, and current drawdown similarity.
- Build a three-leg synthetic basket from the seed plus the best two peers.
- Render the result in the same full-screen candle view and keep streaming/order preview behavior unchanged.

## Testing

- Add a unit-level regression that a Nike seed produces a three-component basket containing `NKE`.
- Keep existing tests for full encrypted stock chunks, duplicate cached dates, build stability, and terminal startup.
