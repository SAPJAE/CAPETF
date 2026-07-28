# Synthetic Margin Preview Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Display Capital.com-derived BUY and SELL margin totals, available account funds, and remaining funds for every synthetic basket preview.

**Architecture:** Extend Capital.com market/account parsing with the metadata needed for margin calculation. Keep arithmetic in a pure `SyntheticMarginCalculator`, use a small async service to refresh account and conversion data, and send one serializable preview model to the existing WebView order ticket.

**Tech Stack:** .NET 8, C# 12, WPF, WebView2, Capital.com REST API, HTML/CSS/JavaScript, existing executable test harness.

## Global Constraints

- Values are estimates derived from current Capital.com market metadata because the official API has no non-trading margin-preview endpoint.
- Existing minimum deal size and size-increment rounding remains authoritative.
- BUY and SELL are calculated independently from effective leg sides and bid/ask prices.
- All summary values use the active Capital.com account currency.
- Missing margin metadata or conversion prices produce `Unavailable`, never zero or a guessed value.
- Preview controls do not submit live orders.
- Account API results are cached for 10 seconds and conversion quotes for 30 seconds.

---

## File Structure

- Modify `desktop/CAPETF.Desktop/Models.cs`: instrument margin metadata and account snapshot models.
- Modify `desktop/CAPETF.Desktop/CapitalApiClient.cs`: parse market margin fields, login account identity, and active account availability.
- Create `desktop/CAPETF.Desktop/SyntheticMarginCalculator.cs`: pure per-leg and basket arithmetic.
- Create `desktop/CAPETF.Desktop/SyntheticMarginPreviewService.cs`: metadata refresh, currency conversion, and caching.
- Modify `desktop/CAPETF.Desktop/SyntheticOrderSizing.cs`: continue using one executable-sizing authority.
- Modify `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`: request and send margin previews.
- Modify `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`: render the compact totals and leg details.
- Modify `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`: parsing, arithmetic, service, and UI contract tests.

### Task 1: Parse Capital.com Margin And Account Metadata

**Files:**
- Modify: `desktop/CAPETF.Desktop/Models.cs`
- Modify: `desktop/CAPETF.Desktop/CapitalApiClient.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Produces: `MarketInstrument.MarginFactor`, `MarketInstrument.MarginFactorUnit`.
- Produces: `CapitalSession.CurrentAccountId`, `CapitalSession.AccountCurrency`.
- Produces: `CapitalAccountSnapshot(string AccountId, string Currency, decimal Available, DateTimeOffset RetrievedAt)`.
- Produces: `Task<CapitalAccountSnapshot> CapitalApiClient.GetActiveAccountAsync(CancellationToken)`.

- [ ] **Step 1: Write failing parsing tests**

Add these tests to the custom test runner:

```csharp
private static void MarketDetailsParseMarginMetadata()
{
    const string json = """
    {
      "instrument": {
        "epic": "SAPD", "name": "SAP", "currency": "EUR",
        "lotSize": 1, "marginFactor": 20, "marginFactorUnit": "PERCENTAGE"
      },
      "snapshot": { "bid": 127.10, "offer": 127.20 }
    }
    """;
    var result = CapitalApiClient.ParseMarketDetails(json)!;
    AssertNear(20m, result.MarginFactor ?? 0m, "margin factor");
    AssertEqual("PERCENTAGE", result.MarginFactorUnit, "margin unit");
}

private static void AccountsParseActiveAvailableFunds()
{
    const string json = """
    { "accounts": [
      { "accountId": "other", "preferred": true, "currency": "GBP",
        "balance": { "available": 50 } },
      { "accountId": "active", "preferred": false, "currency": "USD",
        "balance": { "available": 1250.75 } }
    ] }
    """;
    var result = CapitalApiClient.ParseActiveAccount(json, "active", DateTimeOffset.UnixEpoch);
    AssertEqual("active", result.AccountId, "active account");
    AssertEqual("USD", result.Currency, "account currency");
    AssertNear(1250.75m, result.Available, "available funds");
}
```

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -c Release
```

Expected: compilation fails because the new account and margin members do not exist.

- [ ] **Step 3: Implement metadata models and parsing**

Add:

```csharp
public sealed record CapitalAccountSnapshot(
    string AccountId,
    string Currency,
    decimal Available,
    DateTimeOffset RetrievedAt);

public sealed class CapitalSession
{
    // retain existing properties
    public string CurrentAccountId { get; init; } = "";
    public string AccountCurrency { get; init; } = "";
}

public sealed class MarketInstrument
{
    // retain existing properties
    public decimal? MarginFactor { get; set; }
    public string MarginFactorUnit { get; set; } = "";
}
```

Parse `currentAccountId` and `currencyIsoCode` from login JSON; parse `marginFactor` and `marginFactorUnit` from `instrument`. Implement `GetActiveAccountAsync` against `api/v1/accounts`. Match `CurrentAccountId`, fall back to `preferred: true`, and throw a clear error if neither exists.

- [ ] **Step 4: Run tests and verify GREEN**

Run the command from Step 2. Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop/Models.cs desktop/CAPETF.Desktop/CapitalApiClient.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "feat: read Capital margin and account metadata"
```

### Task 2: Calculate Executable BUY And SELL Margin

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticMarginCalculator.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticOrderSizing.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticOrderSizing.BuildExecutableOrderPreview(SyntheticBasket, string, decimal)`.
- Produces: `SyntheticMarginLegPreview`, `SyntheticMarginSidePreview`, and `SyntheticMarginSummary`.
- Produces: `SyntheticMarginCalculator.CalculateSide(SyntheticBasket, string, decimal, string, decimal)`.
- Produces: `SyntheticMarginCalculator.Combine(CapitalAccountSnapshot, SyntheticMarginSidePreview, SyntheticMarginSidePreview)`.

- [ ] **Step 1: Write failing arithmetic tests**

Test percentage factors, distinct BUY/SELL prices, negative formula legs, lot size, minimum-size rounding, conversion, and remaining funds. Core case:

```csharp
var basket = new SyntheticBasket { Symbol = "SYN-MARGIN" };
basket.Components.Add(new SyntheticComponent(new MarketInstrument {
    Epic = "LONG", Currency = "EUR", Bid = 99m, Offer = 101m,
    LotSize = 1m, MinDealSize = 1m, MinSizeIncrement = 1m,
    MarginFactor = 20m, MarginFactorUnit = "PERCENTAGE"
}, 50m, 0m, 0m) { FormulaMultiplier = 0.5m });
basket.Components.Add(new SyntheticComponent(new MarketInstrument {
    Epic = "HEDGE", Currency = "EUR", Bid = 49m, Offer = 51m,
    LotSize = 1m, MinDealSize = 1m, MinSizeIncrement = 1m,
    MarginFactor = 25m, MarginFactorUnit = "PERCENTAGE"
}, 50m, 0m, 0m) { FormulaMultiplier = -0.5m });

var buy = SyntheticMarginCalculator.CalculateSide(basket, "BUY", 300m, "USD", 1.10m);
AssertEqual("BUY", buy.Legs[0].Side, "positive leg side");
AssertEqual("SELL", buy.Legs[1].Side, "negative leg reverses side");
AssertNear(buy.Legs.Sum(x => x.MarginAccountCurrency), buy.TotalMargin, "total");
```

Add an unsupported-unit test expecting an unavailable result naming the epic. Add a `Combine` test proving `AfterBuy = Available - Buy.TotalMargin` and the equivalent SELL result.

- [ ] **Step 2: Run tests and verify RED**

Run the full test executable. Expected: compilation fails because the calculator and records are absent.

- [ ] **Step 3: Implement the pure calculator**

Use the existing executable preview as the sole source of quantity, reference price, and effective side:

```csharp
var lotSize = instrument.LotSize is > 0 ? instrument.LotSize.Value : 1m;
var nativeNotional = leg.Quantity * leg.ReferencePrice * lotSize;
var nativeMargin = nativeNotional * instrument.MarginFactor!.Value / 100m;
var accountMargin = nativeMargin * conversionRate;
```

Require `MarginFactorUnit == "PERCENTAGE"` case-insensitively. Keep full decimal precision in C# and format only in the UI. Include native currency/margin, account currency/margin, and effective side on each leg.

- [ ] **Step 4: Run tests and verify GREEN**

Run the full test executable. Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop/SyntheticMarginCalculator.cs desktop/CAPETF.Desktop/SyntheticOrderSizing.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "feat: calculate synthetic basket margin"
```

### Task 3: Resolve Account Currency And Refresh Margin Preview

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticMarginPreviewService.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop/Models.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: account, market-detail, and market-search API methods.
- Produces: `Task<SyntheticMarginSummary> SyntheticMarginPreviewService.BuildAsync(SyntheticBasket, decimal, CancellationToken)`.
- Produces: `window.setTerminalMarginPreview(<summary JSON>)`.

- [ ] **Step 1: Write failing service and integration tests**

Introduce an injectable source:

```csharp
internal interface ISyntheticMarginDataSource
{
    Task<CapitalAccountSnapshot> GetActiveAccountAsync(CancellationToken cancellationToken);
    Task<MarketInstrument?> GetMarketDetailsAsync(string epic, CancellationToken cancellationToken);
    Task<IReadOnlyList<MarketInstrument>> SearchMarketsAsync(string query, CancellationToken cancellationToken);
}
```

Test same-currency rate `1`, direct `EUR/USD` midpoint, inverse-pair reciprocal, account caching, and missing conversion failure. A fake source returning EUR/USD bid `1.09` and offer `1.11` must produce `1.10`.

Add source-contract assertions that the window handles `previewMargins`, invokes `BuildAsync`, toggles terminal busy state, and calls `window.setTerminalMarginPreview`.

- [ ] **Step 2: Run tests and verify RED**

Run the full test executable. Expected: compilation and source-contract failures for the missing service.

- [ ] **Step 3: Implement orchestration**

Before calculating, refresh missing `LotSize`, deal rules, `MarginFactor`, and `MarginFactorUnit` for every leg. Cache account data for 10 seconds and conversion quotes for 30 seconds.

Conversion algorithm:

1. Return `1m` when basket and account currencies match.
2. Search `${from}/${to}`; select a currency instrument whose normalized symbol/name contains both ISO codes.
3. Fetch details and use bid/offer midpoint.
4. If direct is missing, search `${to}/${from}` and return the reciprocal midpoint.
5. Throw `Margin conversion FROM/TO is unavailable from Capital.com.` if no quote is usable.

Debounce notional and stream-triggered refreshes by 500 ms, cancel superseded requests, and reuse `SetTerminalBusyAsync`.

- [ ] **Step 4: Run tests and verify GREEN**

Run the full test executable. Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop/SyntheticMarginPreviewService.cs desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs desktop/CAPETF.Desktop/Models.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "feat: refresh Capital account margin preview"
```

### Task 4: Render Compact Margin Summary And Publish

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`
- Publish: `desktop/publish/cap.com-terminal-v4-complete/`

**Interfaces:**
- Consumes: `window.setTerminalMarginPreview(summary)`.
- Produces: `previewMargins` WebView messages carrying current basket notional.

- [ ] **Step 1: Write failing HTML contract tests**

Assert stable IDs `buy-margin`, `sell-margin`, `available-margin`, `after-buy-margin`, and `after-sell-margin`; a debounced `previewMargins` message on notional changes; and `Unavailable` rendering for missing values.

- [ ] **Step 2: Run tests and verify RED**

Run the full test executable. Expected: source-contract failure because the elements and JS function are absent.

- [ ] **Step 3: Implement the order-ticket UI**

Add this unframed summary above the preview buttons:

```html
<div id="margin-summary" aria-live="polite">
  <div><span>Buy margin</span><strong id="buy-margin">Unavailable</strong></div>
  <div><span>Sell margin</span><strong id="sell-margin">Unavailable</strong></div>
  <div><span>Available</span><strong id="available-margin">Unavailable</strong></div>
  <div><span>After Buy</span><strong id="after-buy-margin">Unavailable</strong></div>
  <div><span>After Sell</span><strong id="after-sell-margin">Unavailable</strong></div>
</div>
```

Format with the account ISO currency, apply the existing warning color to negative remaining funds, mark stale availability, and preserve detailed leg rows below. Trigger refresh after basket load, notional input, and live tick update.

- [ ] **Step 4: Run automated verification and publish**

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -c Release
dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release --no-restore
dotnet publish desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release -r win-x64 --self-contained true -o desktop/publish/cap.com-terminal-v4-complete
```

Expected: tests pass and build/publish finish with zero errors and zero warnings.

- [ ] **Step 5: Exercise the published app against Capital.com demo**

Launch `desktop/publish/cap.com-terminal-v4-complete/CAPETF.exe`, connect, and build a three-leg basket. Verify non-zero BUY/SELL margin, current available funds, exact subtraction for After Buy/Sell, notional-driven recalculation, visible busy state, and no submitted order.

- [ ] **Step 6: Commit**

```powershell
git add desktop/CAPETF.Desktop/Assets/synthetic-terminal.html desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs desktop/publish/cap.com-terminal-v4-complete
git commit -m "feat: show buy and sell margin totals"
```

