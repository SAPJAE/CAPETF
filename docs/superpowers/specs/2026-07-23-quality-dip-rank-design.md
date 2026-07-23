# Quality Dip Rank Design

## Purpose

Add a price-based ranking that finds Capital.com stocks whose long-term price
history was consistently rising but whose current price has fallen to an
unusually attractive level.

The rank is a technical opportunity screen, not a measure of company quality or
intrinsic value. Capital.com remains the only data source.

## Scope

- Apply the new rank to stocks.
- Keep ETF ranking behavior unchanged.
- Keep Performance Rank and Investment Rank unchanged.
- Use only Capital.com instrument metadata and historical prices.
- Do not use external fundamentals, websites, or additional API keys.

## Eligibility

A stock receives a Quality Dip score only when it has:

- at least three years of usable weekly price history;
- at least 120 weekly observations;
- a current Capital.com market mode that is not permanently view-only; and
- enough recent data to calculate the stabilization measures.

Stocks with insufficient history display `Quality dip: Unrated` and sort after
scored stocks.

## Score

The score ranges from 0 to 100 and has four components.

### 1. Long-term trend quality: 40 points

- Five-year or maximum-available annualized return: 12 points.
- Positive calendar-year ratio: 10 points.
- Positive slope of a log-price regression: 10 points.
- Regression fit (`R-squared`), rewarding a smooth rise: 8 points.

This component uses weekly log prices so that early low prices and later high
prices are compared proportionally.

### 2. Discount: 30 points

- Drawdown from the trailing 52-week high: 12 points.
- Percentage below the fitted long-term trend line: 10 points.
- Proximity to the previous calendar year's low: 8 points.

The discount component reaches its useful range for material pullbacks. A tiny
decline does not rank highly, while an extreme collapse receives no automatic
advantage.

### 3. Stabilization: 20 points

- Price has stopped making new 13-week lows: 6 points.
- Four-week return is improving: 5 points.
- Price has reclaimed or is approaching the 10-week average: 5 points.
- Ten-week average slope is flattening or rising: 4 points.

### 4. Risk control: 10 points

- Drawdown is smaller than the stock's worst historical drawdown: 4 points.
- Recent weekly volatility is not abnormally high relative to its own history:
  3 points.
- The latest Capital.com spread is not abnormally wide when bid and offer data
  are available: 3 points.

## Penalties and Gates

- If the long-term trend-quality component is below 22/40, cap the final score
  at 49.
- If the 40-week average is falling sharply, subtract 15 points.
- If the stock made a fresh 52-week low in the latest two weeks and has no
  stabilization evidence, subtract 15 points.
- If the current drawdown exceeds 70%, cap the score at 55.
- A stock cannot receive `Confirmed` unless its long-term trend score is at
  least 28/40 and stabilization is at least 14/20.

These rules prevent a collapsing stock from winning simply because it is far
below its previous high.

## Labels

- `80-100`: Confirmed quality dip
- `65-79.99`: Stabilizing quality dip
- `50-64.99`: Watch
- `0-49.99`: Broken or weak trend
- No score: Unrated

The phrase `quality dip` refers only to the quality of the historical price
trend.

## Dashboard

Add the following stock sort options:

- Quality dip, best to worst
- Largest discount in quality trend
- Stabilizing dips

Each stock tile shows:

- Quality Dip Rank
- Quality Dip Score
- Long-term trend score
- Discount score
- Stabilization score
- Current drawdown from the 52-week high
- Distance from the fitted trend
- Quality Dip label

The default stock sort remains the user's current choice. The new rank is
selected explicitly.

## Data and Refresh

The existing Capital.com stock refresh calculates the score after historical
prices are loaded. The generated fields are stored with each stock record so
the browser only sorts and renders; it does not recalculate the statistical
model.

Ranks are calculated across all validated stocks after all batches have been
combined. Partial batches may show scores but must not show a final global rank.

## Failure Handling

- Missing history produces `Unrated`, not a zero score.
- Missing bid/offer data removes the spread subscore and proportionally
  normalizes the risk-control component.
- Invalid or stale histories are excluded from ranking and marked with the
  existing validation state.
- Every generated dataset records the scoring version to make later formula
  changes auditable.

## Verification

- Unit-test smooth uptrend, uptrend-then-dip, persistent collapse, sideways,
  volatile spike, and insufficient-history fixtures.
- Confirm that an uptrend-then-dip fixture outranks both a smooth stock near its
  high and a persistent collapse.
- Confirm deterministic scores and ranks across batch ordering.
- Verify the new controls and tile fields on desktop and mobile layouts.
