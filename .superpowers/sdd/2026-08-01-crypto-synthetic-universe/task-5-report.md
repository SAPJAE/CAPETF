# Task 5 Report: Crypto History, Realtime, And Trade Workspace

## Status

Implemented Task 5 from base commit `844adc8`.

Manual ETH/BTC baskets now use the complete shared Capital.com history, subscribe both component epics for quotes and native OHLC bars, update the current direct-formula candle without truncating history, preserve streaming across saved-basket and timeframe reloads, and carry an execution-specific identity through the existing chart and trade dock so entry, broker SL/TP, PLAN SL/TP, pending orders, and broker P/L remain linked.

No Capital.com credentials were read. No connection was opened and no order was submitted.

## Architecture And File Mapping

The Task 5 brief named `SyntheticTradeWorkspace.cs` and `TerminalChartHost.cs`, but those files do not exist at base `844adc8`. Their current responsibilities live in `SyntheticTerminalModels.cs`, `SyntheticTerminalChartPayload.cs`, `SyntheticTerminalLiveUpdate.cs`, `CapComTerminalWindow.xaml.cs`, and `Assets/synthetic-terminal.html`. Task 5 extends those current shared paths and does not add replacement chart, ledger, or execution systems.

## Changed Files

- `desktop/CAPETF.Desktop/SyntheticRealtimeBarBuilder.cs` (new): intersects current component bars by timeframe bucket and updates/appends only the current direct-formula synthetic candle.
- `desktop/CAPETF.Desktop/CapitalStreamingClient.cs`: parses Capital `ohlc.event` payloads and publishes `OhlcReceived` beside the existing quote event.
- `desktop/CAPETF.Desktop/Models.cs`: adds the immutable Capital OHLC update model.
- `desktop/CAPETF.Desktop/ManualSyntheticBasketFactory.cs`: shares the historical direct-formula candle calculator with realtime bars.
- `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`: subscribes quote and native OHLC streams, routes bars through the shared chart tick bridge, preserves execution identity through timeframe reloads, and restores manual baskets/executions against the Crypto universe.
- `desktop/CAPETF.Desktop/SyntheticTerminalChartPayload.cs`: accepts the active workspace drawing identity.
- `desktop/CAPETF.Desktop/SyntheticTerminalLiveUpdate.cs`: carries that identity through quote and OHLC tick payloads.
- `desktop/CAPETF.Desktop/SyntheticTerminalModels.cs`: defines the existing trade workspace's execution drawing identity.
- `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`: deterministic ETH/BTC history, formula OHLC, quotes, realtime bars, timeframe, saved restore, and subscription coverage.
- `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`: deterministic manual execution ledger, pending-order, entry, P/L, broker/PLAN risk overlay, and live identity coverage.
- `.superpowers/sdd/2026-08-01-crypto-synthetic-universe/task-5-report.md`: this report.

No HTML trade workspace changes were needed; the deterministic manual fixture proves the existing generic dock and overlay functions accept the two-leg `9`/`0.2` execution unchanged.

## TDD Red Evidence

After adding the deterministic fixtures and correcting test-helper-only compile issues, the focused command was:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- manual-formula
```

Observed result:

```text
Exit code: 1
SyntheticTerminalWorkspace does not contain ExecutionDrawingIdentity
SyntheticRealtimeBarBuilder could not be found
CapitalOhlcUpdate could not be found
CapitalStreamingClient does not contain ParseOhlcUpdate
The build failed.
```

The red failed only because the requested OHLC event, ongoing-bar, and execution-chart identity contracts did not exist.

The trading route was also red on the same missing execution identity contract:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading
```

```text
Exit code: 1
SyntheticTerminalWorkspace does not contain ExecutionDrawingIdentity
The build failed.
```

## Deterministic ETH/BTC Evidence

The historical fixture contains three ETH candles and two BTC candles with only two shared timestamps. The basket retains exactly those two timestamps and computes every OHLC field directly:

```text
Shared candle 1: O 1100, H 1200, L 1000, C 1149
Shared candle 2: O 2940, H 3222, L 2658, C 3081
```

With ETH `1999 / 2001` and BTC `29990 / 30010`:

```text
Bid = 9 * 1999 + 0.2 * 29990 = 23989
Ask = 9 * 2001 + 0.2 * 30010 = 24011
```

A new ETH tick updates those formula quotes and appends one ongoing daily candle. Matching ETH/BTC OHLC events then replace only that ongoing candle with direct formula values while the complete historical prefix remains byte-for-byte equal. A repeated same-bucket ETH bar recomputes the final candle without adding a duplicate.

Weekly, Daily, and 4H fixtures rebuild the same symbol, strategy, epic order, and exact `9`/`0.2` multipliers with 5, 7, and 9 longest shared candles respectively. The native stream resolutions are `WEEK`, `DAY`, and `HOUR_4`.

The trade fixture loads drawing identity `execution-manual-crypto-1` and proves:

```text
Entry 24000
Bid 23890
Ask 24000
Running P/L -8.25
Broker SL 22700
Broker TP 26400
PLAN SL 22500
PLAN TP 26500
```

Both ETH and BTC pending orders remain visible through the existing pending-order dock route.

## Verification

Focused manual formula:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- manual-formula
```

```text
Exit code: 0
Wall time: 8.3 seconds
ManualFormula tests passed
```

Focused Crypto universe:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- crypto-universe
```

```text
Exit code: 0
Wall time: 3.9 seconds
CryptoUniverse tests passed
```

Focused trading:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading
```

```text
Exit code: 0
Wall time: 11.8 seconds
SyntheticTrading tests passed
```

Full builder regression route:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- builder
```

```text
Exit code: 0
Wall time: 36.8 seconds
SyntheticBasketBuilder tests passed
```

Full suite:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj
```

```text
Exit code: 0
Wall time: 39.6 seconds
SyntheticTrading tests passed
SyntheticBasketBuilder tests passed
```

Release build:

```powershell
dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release
```

```text
Exit code: 0
Wall time: 3.4 seconds
Build succeeded.
0 Warning(s)
0 Error(s)
```

Whitespace:

```powershell
git diff --check
```

```text
Exit code: 0
No whitespace errors. Git reported only LF-to-CRLF working-copy conversion warnings.
```

## Self-Review

- Confirmed historical and realtime manual OHLC call the same direct signed-formula calculator; there is no rebasing or equal-notional rewrite.
- Confirmed a realtime candle is emitted only after every current basket epic has the same native timeframe bucket.
- Confirmed stale component bars, wrong resolutions, unrelated epics, and incomplete component sets cannot mutate the basket.
- Confirmed same-bucket updates replace only the final candle and newer buckets append exactly one candle; earlier history is never cleared or rebuilt by a tick.
- Confirmed 4H uses Capital's native `HOUR_4` stream while full 4H REST history keeps the existing deep hourly aggregation path.
- Confirmed 2H and 6H retain their existing quote-bucket ongoing-candle behavior; native OHLC mutation is limited to 4H, Daily, and Weekly.
- Confirmed manual builds, saved restores, execution restores, timeframe reloads, and reconnects all call the existing shared streaming start path.
- Confirmed a saved/manual execution first selects and loads Crypto when the terminal was previously on Stocks; automatic strategy restore behavior is unchanged.
- Confirmed execution-specific drawing identity survives full payload render, quote ticks, OHLC ticks, and timeframe reloads.
- Confirmed the existing broker snapshot, execution ledger, pending-order table, risk-plan store, and chart price-line functions are reused without a second execution path.
- Confirmed full builder regression coverage passed, including existing Stock and ETF behavior.
- Confirmed no credential, API connection, confirmation, or order execution path was invoked.

## Commit

- Branch: `feature/cap-com-terminal-v4`
- Base: `844adc844aa58ff1887cf9befd0fb8ee998e4c2c`
- Subject: `Stream and trade manual crypto baskets`
- The final commit hash is returned in the task response because a commit cannot contain its own hash.

## Concerns And Boundaries

- Automated validation used deterministic fixtures only. Live Capital.com demo stream validation belongs to Task 6.
- Capital provides native `HOUR_4`, `DAY`, and `WEEK` bars. The existing 2H/6H quote-driven bucket path remains in place because Capital has no native 2H/6H OHLC stream resolution.
- Streaming subscriptions remain session-scoped and reuse the existing reconnect behavior. This task does not introduce credentials, unattended execution, or any order mutation.
