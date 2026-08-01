# ETH/BTC Demo Basket Validation

Date: 2026-08-01

## Scope

This record covers automated verification, publishing, and hands-on validation of the published Windows application against an authenticated Capital.com demo account. Existing unrelated demo positions were observed but not changed or closed. No crypto order has been submitted because the final transaction requires action-time user confirmation.

Validated commit: `05f4c19` (`Bound submitted position recovery window`)

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

## Hands-On Validation Status

The published executable was launched and driven in a locally authorized Capital.com demo session.

- PASS: connected to Capital.com demo and loaded the API-backed Crypto universe.
- PASS: selected `Crypto / USD / All`, chose Manual formula, retained `9 ETHUSD + 0.2 BTCUSD`, and built the two-leg synthetic.
- PASS: validated nonzero live Bid/Ask, shared history, interactive chart, formula display, dealing rules, available margin, ratio-valid quantity, prechecks, and trade dock account state.
- PASS: recorded epics, deal rules, basket quantity, leg quantities, prices, and margin without recording credentials or account identifiers.
- PENDING: obtain action-time user confirmation and submit the final demo order.
- PENDING: after confirmed submission, verify both broker positions correlate to the synthetic execution, running P/L, and chart overlays. Positions must remain open.

No API secret, password, token, or account identifier is recorded in this document.

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

The confirmation dialog was opened for inspection only. No order was submitted; action-time user confirmation remained required before the final demo execution click.

## Final Published-Binary Validation

The final self-contained publish was rebuilt from commit `05f4c19` and then launched directly from the publish folder.

| Evidence | Observed result |
| --- | --- |
| Executable | `desktop/publish/cap.com-terminal-v4-complete/CAPETF.exe` |
| Size | 151,552 bytes |
| Last-write UTC | `2026-08-01T15:00:05.0574596Z` |
| SHA-256 | `CF83276476794C91F9DC4B978ED74FE9F4D06B81AB771FE022E60632171ECABC` |
| Publish shape | 495 files; zero `.zip` files |
| Independent safety review | No Critical or Important findings after the submitted-position recovery window fix |

### Final Runtime Evidence

- Capital.com demo connected successfully and reported account state, open positions, pending orders, available funds, and margin. Existing PLTR and HOOD positions remained untouched.
- The final Crypto load returned 229 currently eligible instruments after metadata enrichment and openability filtering.
- `Crypto / USD / All` resolved `ETHUSD` and `BTCUSD` and built `SYN-CRYPTO-ETHBTC-01` from the exact formula `9 ETHUSD + 0.2 BTCUSD`.
- Weekly intersection history contained 574 candles from 2015-08-06 through 2026-07-27. The interactive chart rendered that history with moving averages, Bid/Ask lines, drawing tools, zoom, pan, and live synthetic quote updates.
- The formula pane showed ETH minimum/step `0.001` and BTC minimum/step `0.0001`.
- The application automatically selected the smallest exact ratio-preserving basket quantity `0.001`, producing `0.009 ETHUSD` and `0.0002 BTCUSD`.
- A fresh live BUY preflight showed nonzero ETH/BTC prices, synthetic Bid/Ask, estimated margin `USDd 14.74`, available margin `USDd 20,786.53`, and after-buy availability `USDd 20,771.79`.
- The frozen ticket required acknowledgement of partial-execution risk and kept `Confirm Demo Order` disabled until acknowledgement.

### Final Safeguards Included

- Each leg is revalidated immediately before execution for current quote age, market status, minimum size, increment, maximum size, allowed direction, and available margin.
- Crypto metadata requests use global pacing, bounded concurrency, and bounded retry for transient HTTP 429 failures.
- Intraday aggregation aligns each instrument to deterministic fixed UTC buckets and discards incomplete buckets before intersection.
- Saved manual baskets include exact canonical multipliers in their identity, so different ratios cannot overwrite one another.
- Submitted-leg recovery requires one unambiguous exact broker position within a fixed two-minute submission window; later matching positions are not claimed.
- Execution-store atomic replacement retries bounded transient Windows/OneDrive file locks.

### Transaction Boundary

The fresh ticket expired while waiting for the required action-time user confirmation. The acknowledgement was not selected and `Confirm Demo Order` was not clicked. Therefore no ETH or BTC order was submitted, and no crypto position exists from this validation run. A new preflight ticket must be generated after explicit confirmation before the final two demo orders can be sent.

## Five-Lot SELL Follow-Up

The user requested five synthetic lots on the SELL side. Entering raw basket quantity `5` was preflighted and correctly rejected before ticket creation: it would produce `45 ETHUSD + 1 BTCUSD`, required approximately `USDd 72,933` margin, and exceeded approximately `USDd 20,787` available margin. No order was sent.

Commit `e06632e` makes the quantity control deal-rule aware. For the exact ETH/BTC basket, one executable synthetic lot is the host-owned minimum basket quantity `0.001`; five lots therefore means basket quantity `0.005`, producing `0.045 ETHUSD + 0.001 BTCUSD`. The terminal now applies that minimum to the input `min` and `step` attributes and displays the current executable-lot count beside the raw basket quantity.

The corrected self-contained build was published unzipped to `desktop/publish/cap.com-terminal-v4-five-lots`. Its executable is 151,552 bytes with SHA-256 `9F8CCC12ECB388C41132B20786174A36B6DDBDF1FF0B04AE4AB4EC7BAF16A836`; the folder contains 494 files and zero `.zip` files. Automated tests passed before publishing.

### Five-Lot Demo Execution

The corrected executable connected to Capital.com demo, loaded 229 eligible Crypto instruments, and rebuilt `SYN-CRYPTO-ETHBTC-01` with 574 shared weekly candles from 2015-08-06 through 2026-07-27. Setting basket quantity `0.005` displayed `5 lots` and produced the required executable SELL legs.

The user selected the partial-execution acknowledgement and submitted the frozen demo ticket. Capital.com accepted both legs:

| Symbol | Side | Quantity | Entry | Deal | State at verification |
| --- | --- | ---: | ---: | --- | --- |
| ETHUSD | SELL | 0.045 | 1835.9500 | `0015421d-0055-311e-0000-0000823b4a3f` | Open |
| BTCUSD | SELL | 0.001 | 62483.5000 | `000940dd-0055-311e-0000-000083749067` | Open |

- Synthetic execution: `fdf9fa06cf5d4a0b8e1b69aaf1d20924`, state `Open`.
- Frozen-ticket estimated SELL margin: `USDd 72.58`.
- Post-trade account snapshot: funds `USDd 20,997.13`, equity `USDd 20,995.44`, available `USDd 20,784.96`, margin used `USDd 210.48`, running P/L `USDd -1.69`.
- The crypto legs initially reported running P/L of `USD -0.07` for ETHUSD and `USD -0.05` for BTCUSD.
- No pending orders appeared.
- Existing PLTR and HOOD positions remained open and unchanged. No position was closed by this validation.
