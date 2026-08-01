# ETH/BTC Demo Basket Validation

Date: 2026-08-01

## Scope

This record covers only automated verification and preparation of the local publish folder. No credentials were used, no connection to Capital.com was attempted, CAPETF was not launched or driven, and no order was submitted.

Base commit: `a09e5c5b7c532d52f09ebb90fb37c588230fac7a`

## Automated Evidence

| Check | Command | Result |
| --- | --- | --- |
| Test suite | `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj` | PASS. Output: `SyntheticTrading tests passed`; `SyntheticBasketBuilder tests passed`. Exit code 0. |
| Release build | `dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release` | PASS. Exit code 0; 0 warnings; 0 errors. |
| Self-contained publish | `dotnet publish desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release -r win-x64 --self-contained true -o desktop/publish/cap.com-terminal-v4-complete` | PASS. Exit code 0. |
| Publish shape | Filesystem inspection | PASS. `CAPETF.exe` exists; 495 files are present; zero `.zip` files were found. |
| Final test suite | `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj` | PASS. Output: `SyntheticTrading tests passed`; `SyntheticBasketBuilder tests passed`. Exit code 0. |

Before publishing, the only detected `CAPETF.exe` process was running from the exact target publish directory. That target process (PID 18492) was stopped; no unrelated process was targeted. A follow-up process check found zero target-directory CAPETF processes.

## Publish Details

- Publish directory: `desktop/publish/cap.com-terminal-v4-complete`
- Executable: `desktop/publish/cap.com-terminal-v4-complete/CAPETF.exe`
- Executable size: 151,552 bytes
- Runtime: self-contained `win-x64`
- Publish directory is ignored by `.gitignore` via `desktop/**/publish/`.

## Pending Hands-On Validation

The controller must perform and record the following in a locally authorized CAPETF session. These checks have not been performed by this task.

- Launch the published executable and connect to Capital.com demo.
- Select Crypto and `Crypto / USD / All`, choose Manual formula, load `9 ETHUSD + 0.2 BTCUSD`, and build.
- Validate nonzero current Bid/Ask, shared history, chart interaction and timeframes, a live ongoing candle, formula display, dealing rules, available margin, the smallest ratio-valid quantity, trade prechecks, and trade dock state.
- Record actual epics, deal rules, basket quantity, leg quantities, prices, margin, and screenshots.
- At the irreversible demo-order confirmation boundary, obtain action-time user confirmation before submitting any demo order.
- After confirmed submission only, poll confirmations and positions; verify both legs correlate to one synthetic execution, running P/L, and chart entry/SL/TP plan overlays. Do not close positions automatically.

No live validation result, account information, API secret, token, order, position, price, margin value, or screenshot is asserted in this document.

## Live Validation Round 1: Crypto Metadata Blocker and Fix

### Controller-Observed Blocker

The controller connected to Capital.com demo and reported that Crypto loaded 287 instruments, but BlockBox contained only `Crypto / Currency / All`. Summary market rows had blank quote currency, so `Crypto / USD / All` could not be selected and the ETH/BTC manual preset could not be reliably resolved. This task did not reconnect or otherwise interact with Capital.com.

### Root Cause and Contract Evidence

- `CapitalApiClient.ExtractMarkets` already accepts `currency` or `currencyCode` when the all-markets response provides it.
- `LoadUniverseFromApiAsync` previously grouped Crypto summaries immediately, without requesting detail metadata. `TerminalCryptoUniverseGrouping` consequently placed blank currencies in `Crypto / Currency / All`.
- Capital.com's public API reference documents `GET /markets` as the all-markets/search response and `GET /markets/{epic}` as the single-market response. The single-market response supplies `instrument.currency`, `marginFactor`, and `dealingRules`, while the documented all-markets sample does not guarantee a currency field. No local Postman collection was found in this repository.

### Implemented Fix

- Added `CryptoMarketMetadataEnricher`: only Crypto summaries with blank quote currency are enriched through the existing authenticated detail client, with a maximum of four concurrent requests.
- Successful details are cached by epic for the current application session; the cache is recreated after each login. Duplicate epics share a request.
- Detail currency, status, lot/dealing rules, and margin metadata are merged into the summary. Summary quote values remain preferred when present.
- Per-market detail failures leave the summary unresolved and do not abort loading. Cancellation propagates. The existing Crypto eligibility filter is reapplied after detail merge, so detail-provided `CLOSE_ONLY`/other non-openable states remain excluded.
- Progress reports `Loading Crypto market details` while enrichment is active. Enriched rows normalize into `Crypto / USD / All`, where the manual `9 ETHUSD + 0.2 BTCUSD` preset resolves actual `ETH/USD` and `BTC/USD` fixtures.

### Strict TDD Evidence

| Stage | Command | Result |
| --- | --- | --- |
| RED | `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- crypto-universe` | Expected FAIL before production code: `crypto metadata enricher must exist`. Exit code 1. |
| Focused GREEN | `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- crypto-universe` | PASS. `CryptoUniverse tests passed`. Exit code 0. |
| Full suite | `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj` | PASS. `SyntheticTrading tests passed`; `SyntheticBasketBuilder tests passed`. Exit code 0. |
| Release build | `dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release` | PASS. Exit code 0; 0 warnings; 0 errors. |

Focused fixtures cover blank summary USD/EUR currency recovery, exact ETH/BTC preset resolution, duplicate/request-cache behavior, individual failure tolerance, cancellation, progress, bounded concurrency, and post-detail non-openable filtering.

### Publishing State

The existing publish folder has not been modified in this round. Publishing is pending controller confirmation that every CAPETF.exe instance from the target publish directory has been closed.
