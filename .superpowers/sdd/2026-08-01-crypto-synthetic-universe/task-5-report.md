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

---

# Fix Round 1: Review Corrections

## Status

Fixed all three Important review findings and replaced the weak manual restore/streaming source checks with behavioral coverage. The fix base is `85087bc`.

Manual historical candles now intersect only exact UTC instants at every resolution. Native OHLC mutation and subscriptions are limited to active `ManualFormula` baskets at `4H`, `Daily`, and `Weekly`; automatic Stock and ETF baskets remain quote-only. Saved baskets persist an optional universe identity, legacy JSON infers Crypto from the `Crypto /` block/manual strategy and ETFs from known ETF membership, and saved/open basket restoration loads the inferred universe before resolving legs in both directions.

No credentials were read, no Capital.com connection was opened, and no order path was invoked.

## Red Evidence

Initial behavioral contracts were absent:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- manual-formula
```

```text
Exit code: 1
SavedSyntheticBasket has no UniverseKind parameter/property
SyntheticBasketUniverseResolver does not exist
TerminalUniverseUiCoordinator has no EnsureActiveAsync
SyntheticStreamingSubscription does not exist
```

After adding only those contracts, the exact-history fixture exposed the existing daily bucketing bug:

```text
Exit code: 1
Expected 2026-07-20T00:00:00Z
Got 2026-07-06T00:00:00Z | 2026-07-13T00:00:00Z | 2026-07-20T00:00:00Z
```

After correcting the factory intersection, the automatic-basket fixture exposed native OHLC mutation:

```text
Exit code: 1
a complete automatic OHLC component set must still be ignored
```

The load-boundary fixture was then red on the missing exact manual merger, and the automatic streaming fixture was red until automatic baskets returned to quote-only behavior:

```text
Exit code: 1
SyntheticHistoryService does not contain MergeSelectedManualHistory

Exit code: 1
automatic stock streaming remains quote-only. Expected 1, got 2
```

## Files

- `desktop/CAPETF.Desktop/ManualSyntheticBasketFactory.cs`: exact UTC tick keys for every manual historical resolution and exact shared-range validation.
- `desktop/CAPETF.Desktop/SyntheticHistoryService.cs`: separate exact-timestamp selected-history merge for manual baskets; automatic daily/weekly alignment remains unchanged.
- `desktop/CAPETF.Desktop/SyntheticRealtimeBarBuilder.cs`: `ManualFormula` and supported-resolution guard at the OHLC mutation boundary.
- `desktop/CAPETF.Desktop/SyntheticStreamingSubscription.cs`: shared quote subscription plus manual-only native OHLC subscription using the existing client methods.
- `desktop/CAPETF.Desktop/SavedSyntheticBasketStore.cs`: optional persisted `UniverseKind`, compatible with legacy JSON that omits it.
- `desktop/CAPETF.Desktop/SyntheticBasketUniverseResolver.cs`: saved/open universe inference from explicit metadata, Crypto block/manual identity, and known ETF membership.
- `desktop/CAPETF.Desktop/TerminalUniverseUiCoordinator.cs`: behavioral select-clear-load coordination before leg resolution.
- `desktop/CAPETF.Desktop/ActiveSyntheticBasketState.cs`: passes optional universe metadata into saved baskets.
- `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`: uses exact manual history, persists/restores universe identity, and routes subscriptions through the shared coordinator.
- `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`: mismatched same-date/week fixtures, Stock/ETF OHLC isolation, socket-observed subscriptions/resolution changes, legacy JSON, persisted universe, and bidirectional saved/open restoration.
- `.superpowers/sdd/2026-08-01-crypto-synthetic-universe/task-5-report.md`: this Fix Round 1 report.

## Verification

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- manual-formula
```

```text
Exit code: 0; 3.8 seconds
ManualFormula tests passed
```

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- crypto-universe
```

```text
Exit code: 0; 6.1 seconds
CryptoUniverse tests passed
```

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading
```

```text
Exit code: 0; 6.6 seconds
SyntheticTrading tests passed
```

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- builder
```

```text
Exit code: 0; 37.3 seconds
SyntheticBasketBuilder tests passed
```

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj
```

```text
Exit code: 0; 38.5 seconds
SyntheticTrading tests passed
SyntheticBasketBuilder tests passed
```

```powershell
dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release
```

```text
Exit code: 0; 3.6 seconds
Build succeeded.
0 Warning(s)
0 Error(s)
```

```powershell
git diff --check
```

```text
Exit code: 0
No whitespace errors. Git emitted only working-copy LF-to-CRLF conversion warnings.
```

## Self-Review

- Confirmed manual history uses exact UTC ticks in both API/cache merge and direct formula construction for intraday, Daily, and Weekly resolutions.
- Confirmed same calendar date or week is insufficient: ETH `00:00Z` and BTC `12:00Z` never combine, while the one exact fixture timestamp remains.
- Confirmed realtime bucket alignment remains intentional and isolated to ongoing manual bars.
- Confirmed automatic Stock and ETF baskets reject every native OHLC update without changing candles, price, or timestamp, and subscribe only to existing quote streaming.
- Confirmed manual build and restored baskets emit exactly both ETH/BTC epics in quote and OHLC payloads, with `DAY` changing to `HOUR_4` after the timeframe update.
- Confirmed explicit Stock/ETF/Crypto universe metadata wins; legacy manual/Crypto blocks and known ETF membership provide compatible fallback.
- Confirmed Crypto-to-saved/open Stocks, Crypto-to-saved/open ETFs, Stocks-to-saved Crypto, and ETFs-to-open Crypto all select, clear, and load the target universe before leg lookup.
- Confirmed manual trade workspace identity, entry, SL, TP, pending-order, and P/L tests remain green through the existing trading suite.
- Confirmed automatic history alignment and quote update paths were not changed.

## Commit

- Base: `85087bc`
- Subject: `Fix crypto basket integration review issues`
- The final commit hash is returned in the task response because a commit cannot contain its own hash.

## Concerns

- Validation remains deterministic and offline. Live Capital.com demo stream verification is still deferred to Task 6.
- Legacy ETF inference depends on the existing ETF catalog membership; newly saved baskets persist `UniverseKind` and do not depend on that fallback.

---

# Fix Round 2: Legacy ETF Universe Resolution

## Status

Fixed the remaining Important finding from base `ead8dac`.

New baskets, preflight tickets, persisted executions, and execution-derived saved snapshots now freeze `UniverseKind`. Legacy saved baskets and execution records that omit it are resolved from known ETF membership, all available per-universe caches, and Capital instrument type metadata. Unresolved artifacts probe every epic in stable basket order, load the single resolved universe, and merge type-compatible probed instruments before leg restoration. Missing and ambiguous evidence now fail explicitly; there is no silent Stock default.

No credentials were read, no live Capital.com request was made by the deterministic tests, and no order was submitted.

## Red Evidence

The first focused run failed on every missing persistence and resolver contract:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- crypto-universe
```

```text
Exit code: 1
SyntheticExecutionRecord has no UniverseKind parameter
SyntheticBasketUniverseResolver does not contain ResolveAsync
SyntheticExecutionTicket/Record do not expose UniverseKind
SyntheticPreflightInput does not accept universe identity
```

After those contracts were introduced, the build-level fixture independently proved manual baskets had not frozen Crypto identity:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- manual-formula
```

```text
Exit code: 1
manual basket build freezes Crypto universe identity. Expected Crypto, got null
```

These failures were caused by the missing production properties and async evidence resolver, not test setup.

## Behavior

- An uncatalogued API fallback epic with Capital type `ETF` resolves to `TerminalUniverseKind.ETFs` even when `_knownEtfEpics` is empty.
- Capital type metadata takes precedence over a stale per-universe cache label.
- Known ETF catalog membership remains a supported legacy shortcut.
- Legacy saved and open execution forms both probe in exact component/leg order when cache evidence is insufficient.
- A legacy artifact present in multiple candidate caches remains ambiguous if Capital metadata cannot disambiguate it and throws a clear error.
- An artifact absent from caches and Capital metadata throws a clear missing-metadata error.
- Crypto-to-uncatalogued-ETF restoration selects and loads ETFs before existing leg snapshot/history restoration.
- Probed target-universe instruments are merged into the existing universe/cache path; no parallel universe or execution system was added.

## Files

- `desktop/CAPETF.Desktop/SyntheticModels.cs`: adds basket-level optional universe identity.
- `desktop/CAPETF.Desktop/SyntheticTradeModels.cs`: adds compatible optional universe identity to preflight input, ticket, and execution record.
- `desktop/CAPETF.Desktop/SyntheticTradePreflight.cs`: freezes basket/selected universe on the ticket.
- `desktop/CAPETF.Desktop/SyntheticBasketExecutionService.cs`: carries ticket universe into persisted execution records.
- `desktop/CAPETF.Desktop/SyntheticExecutionStore.cs`: validates present universe values while accepting legacy missing values.
- `desktop/CAPETF.Desktop/SyntheticExecutionBasketSnapshot.cs`: carries persisted execution universe into saved snapshots.
- `desktop/CAPETF.Desktop/SavedSyntheticBasketStore.cs`: defaults new saved snapshots to the basket's frozen universe.
- `desktop/CAPETF.Desktop/SyntheticBasketUniverseResolver.cs`: deterministic explicit, catalog, cache, type, probe, missing, and ambiguity resolution.
- `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`: resolves before leg lookup, loads the target universe, merges probed typed legs, and freezes universe at build/preflight/restore.
- `desktop/CAPETF.Desktop/ManualSyntheticBasketFactory.cs`: freezes Crypto identity at manual build.
- `desktop/CAPETF.Desktop/SyntheticPreflightMarketSnapshotLoader.cs`: preserves universe and strategy through fresh detached preflight snapshots.
- `desktop/CAPETF.Desktop/SavedSyntheticBasketRestorer.cs`: preserves saved universe during basket reconstruction.
- `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`: uncatalogued ETF, legacy saved/open, cache metadata, deterministic probe, reverse transition, ambiguity, missing metadata, and legacy JSON coverage.
- `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`: preflight-to-ticket-to-execution-to-store-to-snapshot universe persistence coverage.
- `.superpowers/sdd/2026-08-01-crypto-synthetic-universe/task-5-report.md`: this Fix Round 2 report.

## Verification

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- manual-formula
```

```text
Exit code: 0; 3.7 seconds
ManualFormula tests passed
```

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- crypto-universe
```

```text
Exit code: 0; 3.8 seconds
CryptoUniverse tests passed
```

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading
```

```text
Exit code: 0; 6.2 seconds
SyntheticTrading tests passed
```

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- builder
```

```text
Exit code: 0; 39.4 seconds
SyntheticBasketBuilder tests passed
```

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj
```

```text
Exit code: 0; 38.9 seconds
SyntheticTrading tests passed
SyntheticBasketBuilder tests passed
```

```powershell
dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release
```

```text
Exit code: 0; 3.5 seconds
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Self-Review

- Confirmed new UI-built automatic baskets and factory-built manual baskets receive explicit universe identity before save or preflight.
- Confirmed fresh preflight cloning preserves both strategy and universe, and the ticket, execution record, JSON store, and execution snapshot retain it unchanged.
- Confirmed optional model fields deserialize legacy saved/execution JSON without migration.
- Confirmed explicit persisted universe, manual Crypto identity, and all-known-ETF fallback do not invoke metadata probes.
- Confirmed legacy cache evidence evaluates every epic and uses Capital `ETF`, `SHARES`, and `CRYPTOCURRENCIES` types instead of relying solely on catalog membership.
- Confirmed insufficient cache evidence probes all epics deterministically and authoritative Capital type metadata disambiguates stale cache labels.
- Confirmed only one universe shared by every leg is accepted; missing and ambiguous outcomes identify the affected epic evidence.
- Confirmed target loading and probed instrument augmentation happen before `SyntheticExecutionBasketSnapshot.Create` or saved leg resolution.
- Confirmed the existing history, chart, streaming, ledger, preflight, and execution paths remain in use.
- Confirmed Stock, ETF, Crypto, manual formula, trading, and full builder regressions pass.

## Commit

- Base: `ead8dac`
- Subject: `Fix legacy ETF universe restoration`
- The final commit hash is returned in the task response because a commit cannot contain its own hash.

## Concerns

- Live API fallback restoration still depends on Capital returning instrument type metadata for uncached legacy epics. Missing or contradictory metadata now stops restoration with a clear error instead of selecting Stocks silently.
- Live Capital.com demo verification remains deferred; all automated checks are deterministic and offline.
