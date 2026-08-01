# Crypto Synthetic Universe Implementation Plan

> **For Codex:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Add a complete Capital.com Crypto universe and manual synthetic formula workflow, then validate a demo basket whose one-unit formula is `9 ETH/USD + 0.2 BTC/USD` through history, realtime quotes, margin, preflight, execution, and the trade workspace.

**Architecture:** Extend the existing universe pipeline with API-native crypto discovery and quote-currency grouping. Add a manual basket factory that preserves explicit formula multipliers independently from strategy-selected equal-notional baskets, plus a decimal-only ratio quantity solver that finds the smallest Capital.com-valid basket quantity without changing the requested ratio. Reuse the existing history intersection, WebSocket quote aggregation, saved basket, margin, confirmation, execution ledger, and docked trade workspace.

**Tech Stack:** .NET 8 WPF, Capital.com REST/WebSocket APIs, WebView2, TradingView Lightweight Charts 5.2, System.Text.Json, console-style regression tests.

---

### Task 1: Add crypto instrument classification and API-native discovery

**Files:**
- Modify: `desktop/CAPETF.Desktop/CapitalInstrumentTypes.cs`
- Modify: `desktop/CAPETF.Desktop/TerminalUniverse.cs`
- Modify: `desktop/CAPETF.Desktop/TerminalUniverseLoadPolicy.cs`
- Modify: `desktop/CAPETF.Desktop/CapitalApiClient.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Step 1: Write failing tests**

Add tests proving:

```csharp
CapitalInstrumentTypes.IsCrypto(new MarketInstrument { Type = "CRYPTOCURRENCIES" });
TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, openCrypto);
!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, closeOnlyCrypto);
TerminalUniverseLoadPolicy.ApiSearchTerm(TerminalUniverseKind.Crypto, "BTC") == "";
```

Add an HTTP fixture test that calls `SearchMarketsAsync("")` and verifies the client requests `/api/v1/markets` without a `searchTerm`, then parses BTC/USD and ETH/USD rows with type, currency, status, bid, offer, and epic intact.

**Step 2: Run the focused test and verify it fails**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- crypto-universe`

Expected: FAIL because `Crypto`, `IsCrypto`, and the focused test route do not exist.

**Step 3: Implement the minimum production changes**

Add `Crypto` to `TerminalUniverseKind`, add `CapitalInstrumentTypes.IsCrypto`, and route Crypto through `TerminalUniverse.Accepts`. Preserve the existing open-eligible policy: temporarily closed markets remain visible, while close-only, view-only, reduce-only, disabled, suspended, obsolete, and non-openable markets remain excluded.

Make Crypto API fallback always call all-markets discovery through `SearchMarketsAsync("")`; do not use a user-entered seed as the server-side universe query. Normalize and deduplicate by epic after filtering for `CRYPTOCURRENCIES`.

**Step 4: Run tests**

Run the focused command above, then:

`dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: PASS.

**Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop desktop/CAPETF.Desktop.Tests
git commit -m "Add Capital crypto universe discovery"
```

### Task 2: Load and group Crypto in the terminal UI

**Files:**
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTerminalModels.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Step 1: Write failing tests**

Add tests that assert the XAML contains a Crypto universe item and that crypto instruments are grouped as `Crypto / USD / All`, `Crypto / EUR / All`, or the corresponding quote currency. Verify switching universes uses its own cache, clears the prior basket/chart, and still loads Crypto from the API when no disk cache exists.

**Step 2: Run focused tests**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- crypto-ui`

Expected: FAIL because no Crypto item/group exists.

**Step 3: Implement the UI and grouping**

Add Crypto to `UniverseBox`. Make `SelectedUniverse()` and `UniverseLabel()` exhaustive. In `LoadUniverseAsync`, use the API path directly for Crypto and retain the existing stock/ETF cache behavior. Normalize crypto group metadata from the instrument quote currency, with a deterministic fallback label only when Capital omits currency. Ensure block and seed dropdowns rebuild from the newly selected universe.

**Step 4: Run focused and full tests**

Run the focused command, then the complete test project. Expected: PASS.

**Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop desktop/CAPETF.Desktop.Tests
git commit -m "Expose grouped crypto universe"
```

### Task 3: Add manual synthetic formula construction

**Files:**
- Create: `desktop/CAPETF.Desktop/ManualSyntheticBasketFactory.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticStrategy.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop/SavedSyntheticBasketStore.cs`
- Modify: `desktop/CAPETF.Desktop/SavedSyntheticBasketRestorer.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Step 1: Write failing unit tests**

Define and test:

```csharp
var formula = ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD");
var basket = ManualSyntheticBasketFactory.Create("SYN-ETHBTC-01", "Crypto / USD / All", formula, instruments, candles);
```

Verify exactly two legs, multipliers `9m` and `0.2m`, no equal-notional rewriting, USD block/currency consistency, and clear errors for unknown, duplicate, mixed-currency, zero, or negative legs. Verify save/restore round-trips the manual strategy and exact decimal multipliers.

**Step 2: Run focused tests**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- manual-formula`

Expected: FAIL because the parser/factory/manual strategy do not exist.

**Step 3: Implement the formula model and compact editor**

Add `SyntheticStrategyKind.ManualFormula` and a compact formula input shown only for manual mode. Add a Crypto preset that fills `9 ETHUSD + 0.2 BTCUSD` but remains editable. Resolve tokens against epic, symbol, and name within the selected crypto block, preferring exact epic/symbol matches and refusing ambiguity.

Construct candles through the existing strict timestamp intersection service. Use direct signed formula math for OHLC and Bid/Ask, retaining the source currency and source price scale instead of rebasing the current candle to 100. Save and restore the explicit multipliers unchanged.

**Step 4: Run focused and full tests**

Run both test commands. Expected: PASS.

**Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop desktop/CAPETF.Desktop.Tests
git commit -m "Add manual crypto synthetic formulas"
```

### Task 4: Preserve the exact ratio through Capital.com deal rules

**Files:**
- Create: `desktop/CAPETF.Desktop/RatioPreservingBasketSizer.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticOrderSizing.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTradePreflight.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticMarginCalculator.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticExecutionBasketSnapshot.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Step 1: Write failing sizing and preflight tests**

Test a manual formula where one basket unit means 9 ETH and 0.2 BTC. Given representative Capital dealing rules, assert that `SmallestExecutableQuantity` returns the smallest positive decimal `q` for which both `9q` and `0.2q` satisfy each leg's minimum and increment exactly. Assert an impossible grid returns a blocking error and never rounds each leg independently.

Also assert `BuildExecutableOrderPreview` produces exact ratio-preserving quantities, correct BUY/SELL reference sides, correct notionals/margin, and stores both formula multipliers and basket quantity in the execution snapshot.

**Step 2: Run focused trading tests**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading`

Expected: FAIL on the new manual-ratio cases.

**Step 3: Implement decimal-only ratio sizing**

Add a bounded decimal solver that computes valid basket-quantity increments from each leg's minimum deal size and size step. Manual baskets use `leg quantity = abs(formula multiplier * basket quantity)` exactly. Strategy baskets keep their existing equal-notional sizing. Preflight must block mixed currencies, missing prices, stale/closed/untradeable markets, invalid deal rules, insufficient margin, and any ratio mismatch.

**Step 4: Run focused and full tests**

Run the trading suite, then the complete test project. Expected: PASS.

**Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop desktop/CAPETF.Desktop.Tests
git commit -m "Preserve manual basket execution ratios"
```

### Task 5: Integrate crypto history, realtime quotes, and trade workspace

**Files:**
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop/CapitalStreamingClient.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticRealtimeBarBuilder.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTradeWorkspace.cs`
- Modify: `desktop/CAPETF.Desktop/TerminalChartHost.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Step 1: Add failing integration-style tests**

Use deterministic ETH/BTC fixtures to verify:
- full-history intersection includes only timestamps shared by both legs;
- the synthetic OHLC formula is applied on every shared candle;
- bid is `9 * ETH bid + 0.2 * BTC bid` and ask is `9 * ETH offer + 0.2 * BTC offer`;
- a tick updates quotes and the ongoing candle without replacing historical candles;
- switching Weekly/Daily/4H uses the same basket and reloads the longest available shared history;
- saved/open positions, pending orders, P/L, entry, SL, and TP overlays remain connected to the manual basket identity.

**Step 2: Run focused tests**

Run the crypto/manual focused routes and `-- trading`. Expected: FAIL on new realtime/workspace assertions.

**Step 3: Wire the existing services**

After a manual basket builds, subscribe its two crypto epics automatically. Route live tick and OHLC messages through the current ongoing-bar builder, refresh formula Bid/Ask lines, and keep streaming across saved-basket reloads. Reuse the existing bottom trade dock and chart overlays; add no second execution path.

**Step 4: Run the full suite**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: PASS.

**Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop desktop/CAPETF.Desktop.Tests
git commit -m "Stream and trade manual crypto baskets"
```

### Task 6: Publish and validate the ETH/BTC demo basket end to end

**Files:**
- Modify only if validation exposes defects: `desktop/CAPETF.Desktop/**`
- Create: `docs/validation/2026-08-01-eth-btc-demo-validation.md`
- Output: `desktop/publish/cap.com-terminal-v4-complete/CAPETF.exe`

**Step 1: Run automated verification**

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj
dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release
```

Expected: all tests and build pass.

**Step 2: Publish the unpacked executable**

Close any running CAPETF process, then publish directly to the existing unzipped folder:

```powershell
dotnet publish desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release -r win-x64 --self-contained true -o desktop/publish/cap.com-terminal-v4-complete
```

**Step 3: Perform hands-on demo validation before order submission**

Launch `CAPETF.exe`, connect to Capital.com demo, select Crypto and `Crypto / USD / All`, choose Manual formula, load `9 ETHUSD + 0.2 BTCUSD`, and build. Validate nonzero current Bid/Ask, shared history, chart interaction/timeframes, live ongoing candle, formula display, dealing rules, available margin, smallest ratio-valid quantity, trade prechecks, and trade dock state.

Record actual epics, deal rules, chosen basket quantity, leg quantities, prices, margin, and screenshots in the validation document. If any check fails, fix it and repeat automated and hands-on verification.

**Step 4: Confirm only at the irreversible demo-order boundary**

Immediately before clicking the final confirmation that submits demo orders, request the required action-time user confirmation. After confirmation, submit the smallest ratio-valid BUY basket on the demo account only. Poll Capital confirmations and positions, verify both legs correlate to one synthetic execution, verify running P/L and chart entry/SL/TP plan overlays, and close nothing automatically.

**Step 5: Final verification and commit**

Run the full tests again and document PASS/FAIL evidence. Commit source and validation notes without credentials, tokens, account identifiers, or raw API secrets.

```powershell
git add desktop/CAPETF.Desktop desktop/CAPETF.Desktop.Tests docs/validation
git commit -m "Validate ETH BTC synthetic demo workflow"
```
