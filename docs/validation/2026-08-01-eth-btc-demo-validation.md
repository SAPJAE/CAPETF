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

## Live Validation Round 1 Publish Evidence

The controller confirmed the target application was closed. A local process check then found zero `CAPETF.exe` processes whose executable path was inside the target publish directory before publishing.

| Check | Evidence | Result |
| --- | --- | --- |
| Published commit | `c3769a0 Enrich Crypto market metadata` | Self-contained publish completed. |
| Publish command | `dotnet publish desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release -r win-x64 --self-contained true -o desktop/publish/cap.com-terminal-v4-complete` | PASS. Exit code 0. |
| Publish directory | `desktop/publish/cap.com-terminal-v4-complete` | Exists with 495 files and zero `.zip` files. |
| Executable | `desktop/publish/cap.com-terminal-v4-complete/CAPETF.exe` | Exists; 151,552 bytes; last-write UTC `2026-08-01T12:20:49.5203848Z`; SHA-256 `D6CA577C50046243DC1A863A072B5852236F2BC769D373F77FCEA5424AAE51F9`. |
| Post-publish process check | Target publish directory | PASS. Zero target-directory `CAPETF.exe` processes; the executable was not launched by this task. |

The published build is ready for the controller's live retest of Crypto grouping and the ETH/BTC manual preset. No Capital.com connection, order, or other live-app action was performed by this task.

## Controller Live Validation: Crypto Basket and Preflight

The controller subsequently launched and drove the published Windows application against an authenticated Capital.com demo account. No live account was used. Existing unrelated demo positions were observed but not changed or closed.

### Crypto Discovery and Formula

- Capital.com returned 287 eligible Crypto instruments.
- `Crypto / USD / All` loaded and resolved the API epics `ETHUSD` and `BTCUSD` from the explicit quote pairs `Ethereum/USD` and `Bitcoin/USD`.
- Manual formula `9 ETHUSD + 0.2 BTCUSD` built as `SYN-CRYPTO-ETHBTC-01` with two legs.
- Formula display retained the exact multipliers and showed current notional influence of approximately 57.14% ETH and 42.86% BTC.
- Capital.com dealing rules reported ETH minimum/step `0.001` and BTC minimum/step `0.0001`.
- The smallest exact ratio-preserving basket quantity was `0.001`, producing executable leg sizes `0.009 ETHUSD` and `0.0002 BTCUSD`.

### History and Live Market Data

| View | Observed shared range and count | Result |
| --- | --- | --- |
| Weekly | 2015-08-06 through 2026-07-27; 574 candles | PASS |
| Daily | 2020-07 through 2026-08-01; 1,404 candles | PASS |
| 4H | 2017-05-01 through 2026-08-01; 19,389 candles after full paging | PASS |

Weekly, Daily, and 4H switches preserved the exact formula. The chart retained moving averages, bid/ask lines, drawing controls, interaction, and a live ongoing candle. Synthetic bid and ask remained nonzero and moved with fresh Capital.com WebSocket updates.

### Live Defects Found and Corrected

1. Capital.com market-detail responses for the two crypto epics supplied executable prices and dealing rules but omitted quote timestamps. Preflight originally rejected those undated REST snapshots despite a fresh WebSocket quote. Commit `eb28983` now overlays only a newer same-epic streamed quote onto the current rules snapshot. The five-minute age, future timestamp, positive price, status, margin, and demo gates remain enforced.
2. The demo account initially reported `hedgingMode: false`. Capital.com documents `PUT /accounts/preferences` for changing that preference. Commit `d1e6b72` added a demo-only preference mutation, confirmed by a contract test; live-account preference mutation remains blocked before transport. The controller's next preflight enabled demo hedging and continued.

### Final Frozen Preflight

The host produced a demo-only frozen BUY ticket with:

- basket quantity `0.00100`
- `BUY 0.009 ETHUSD`
- `BUY 0.0002 BTCUSD`
- estimated margin approximately `USDd 14.70`
- available demo funds approximately `USDd 20,786.53`
- explicit acknowledgement that earlier accepted legs remain open if a later leg fails
- a disabled final execution button until that acknowledgement is selected

The confirmation dialog is open. No order has been submitted at the time of this update; action-time user confirmation is still required before the final demo execution click.
