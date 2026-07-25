# Local Synthetic Stock Baskets

## Goal

Add a private local feature to the CAPETF Windows realtime app that builds
synthetic stock symbols from 3 to 4 similar Capital.com stocks. The feature is
for live local analysis with the user's own Capital.com API keys. It must not
depend on the public GitHub Pages dashboard for live API calls.

## User Experience

The Windows app gets a new `Synthetic` tab. The user selects a stock block such
as `US / USD`, `Europe / EUR`, or `UK / GBP`. The app then shows a list of
synthetic symbols. Each symbol contains 3 to 4 stocks, their weights, current
component prices, basket price, and a candlestick chart.

The user can choose a synthetic symbol and inspect:

- component names and Capital.com epics
- calculated weight for each component
- weighted open, high, low, close candles
- recent live quote status from Capital.com
- last update time

## Group Selection

The first version uses the existing stock grouping already present in the app:
region/currency/sector style blocks, with currency treated as a hard boundary
when Capital.com supplies it. Stocks from different known currencies are not
mixed in one synthetic symbol.

Capital.com demo search responses can return stock markets with a blank
currency even when valid OHLC data exists. Those instruments remain eligible
inside the selected UI block and are bucketed separately from known-currency
stocks. They are not mixed with USD, EUR, GBP, or any other known currency.

## Similarity Method

Similarity is based on the last four years of available Capital.com price
history. Stocks are compared using normalized price paths, so a 100 USD stock
and a 20 USD stock can still match if their shapes are similar.

The similarity score uses:

- four-year normalized return path correlation
- annualized volatility over the same period
- one-year, six-month, and three-month returns
- maximum drawdown and current drawdown
- Quality Dip score when it exists

Volatility is the primary constraint. A synthetic group should contain stocks
with similar volatility before optimizing for return-shape similarity.

## Weighting Method

Weights are chosen mainly from similar volatility percentages. The basket should
avoid letting the most volatile component dominate the synthetic candle.

The first version uses inverse-volatility weighting within the selected cluster:

1. Calculate each component's annualized volatility from weekly returns.
2. Convert volatility to a raw weight using `1 / volatility`.
3. Normalize the raw weights to sum to 100%.
4. Cap any single component at 45%.
5. Redistribute capped excess across the remaining components.
6. Require each component to keep at least 10% weight.

This creates synthetic symbols where the lower-volatility stock naturally gets
more weight, while still keeping all 3 to 4 components visible.

## Synthetic Candles

For each timestamp common to all selected components, the app calculates:

- synthetic open = sum(component open * weight)
- synthetic high = sum(component high * weight)
- synthetic low = sum(component low * weight)
- synthetic close = sum(component close * weight)

The candle is an analytical basket approximation, not an exchange-traded
instrument. It is useful for comparing a combined behavior pattern.

## Live Capital.com Data

The feature runs only in the local Windows app. It uses the existing saved local
Capital.com credentials and session handling. Historical candles come from the
Capital.com REST price endpoint. Recent quotes and selected-instrument updates
come from the existing realtime client where available.

No API key, password, CST token, or security token is written to GitHub or to a
public web page.

## Charting

Use TradingView Lightweight Charts inside a local WebView2 chart surface in the
Windows app. The chart page is packaged with the app and receives synthetic
candles from the WPF host; it does not call Capital.com directly.

If WebView2 packaging blocks the installer build, render a native WPF
candlestick chart behind the same chart adapter and keep the data model
unchanged.

The chart should support at least daily and weekly views first. Intraday
synthetic candles can follow once the historical/live pipeline is stable.

## Error Handling

Stocks with missing four-year history can participate only if they have enough
data to calculate a stable volatility estimate. Stocks with invalid prices or
unavailable candles are excluded from that synthetic group. Stocks with missing
currency are kept in a separate fallback bucket for the selected UI block.

If no valid group can be formed for a selected block, the app shows an empty
state with the reason, such as insufficient history or not enough tradable
stocks in the block.

## Testing

Tests should cover:

- inverse-volatility weight calculation, including caps and minimum weights
- grouping without crossing currencies
- synthetic candle calculation from component OHLC rows
- exclusion of invalid or missing price rows
- stable ordering of generated synthetic symbols
