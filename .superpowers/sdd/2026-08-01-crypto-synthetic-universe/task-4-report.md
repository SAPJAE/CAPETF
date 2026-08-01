# Task 4 Report: Ratio-Preserving Manual Basket Execution

## Status

Implemented Task 4 from base commit `27e00ad`.

Manual formula baskets now treat the submitted amount as basket quantity `q`. For the required formula, leg sizes are always derived exactly as:

```text
ETH quantity = abs(9 * q)
BTC quantity = abs(0.2 * q)
```

The implementation finds the smallest shared decimal basket quantity that satisfies every Capital minimum and increment rule, validates a requested quantity without independently rounding any leg, and blocks unsafe preflight states. Automatic strategies continue through the existing equal-notional sizing path.

No Capital.com connection was opened. No demo or live order was submitted.

## Changed Files

- `desktop/CAPETF.Desktop/RatioPreservingBasketSizer.cs` (new): exact decimal-grid solver, requested-quantity validator, bounded decimal representation handling, and leg-specific failures.
- `desktop/CAPETF.Desktop/SyntheticOrderSizing.cs`: manual strategy branch with exact multiplier-derived quantities, side-aware prices, explicit formula multipliers, and basket quantity; automatic equal-notional sizing is unchanged.
- `desktop/CAPETF.Desktop/SyntheticTradePreflight.cs`: manual two-to-four-leg contract, same-currency and dealing-rule validation, ratio failures, exact manual margin identity, and basket-quantity ticket snapshot.
- `desktop/CAPETF.Desktop/SyntheticMarginCalculator.cs`: manual same-currency guard and use of the frozen executable notional.
- `desktop/CAPETF.Desktop/SyntheticExecutionBasketSnapshot.cs`: restores two-to-four-leg manual executions, verifies exact ratio identity, and retains manual strategy, multipliers, and basket quantity.
- `desktop/CAPETF.Desktop/SyntheticTradeModels.cs`: optional basket quantity on immutable execution tickets and persisted execution records.
- `desktop/CAPETF.Desktop/SyntheticBasketExecutionService.cs`: carries basket quantity from ticket into the execution ledger.
- `desktop/CAPETF.Desktop/SyntheticExecutionStore.cs`: rejects non-positive persisted basket quantities while remaining compatible with legacy null values.
- `desktop/CAPETF.Desktop/SavedSyntheticBasketStore.cs`: persists optional basket quantity in execution-derived saved basket snapshots.
- `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`: sizing, BUY/SELL preview, margin, preflight, safety, mismatch, insufficient-funds, and persistence coverage.
- `.superpowers/sdd/2026-08-01-crypto-synthetic-universe/task-4-report.md`: this report.

## TDD Red Evidence

### Initial sizing and snapshot contract red

Command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- trading
```

Observed result:

```text
Exit code: 1
SyntheticTradingTests.cs(1238,24): error CS0103: The name 'RatioPreservingBasketSizer' does not exist in the current context
SyntheticTradingTests.cs(1252,31): error CS1061: 'ExecutableOrderPreview' does not contain a definition for 'BasketQuantity'
SyntheticTradingTests.cs(1260,37): error CS1061: 'ExecutableOrderLegPreview' does not contain a definition for 'FormulaMultiplier'
SyntheticTradingTests.cs(1298,34): error CS1061: 'SyntheticExecutionTicket' does not contain a definition for 'BasketQuantity'
SyntheticTradingTests.cs(1381,13): error CS1739: The best overload for 'SyntheticExecutionRecord' does not have a parameter named 'BasketQuantity'
SyntheticTradingTests.cs(1388,33): error CS1061: 'SavedSyntheticBasket' does not contain a definition for 'BasketQuantity'
The build failed. Fix the build errors and run again.
```

The tests required the absent ratio solver and explicit multiplier/quantity snapshot contracts.

### Exact margin identity red

Self-review identified that preflight matched margin legs only by epic and side. A manual margin snapshot calculated for another basket quantity could therefore be accepted.

Command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- trading
```

Observed result:

```text
Exit code: 1
Unhandled exception. System.Exception: margin for a different quantity must fail preflight
   at CAPETF.Desktop.Tests.SyntheticTradingTests.ManualPreflightRejectsMarginForDifferentBasketQuantity()
```

Manual preflight now requires margin reference price, quantity, and native notional to match the exact executable leg.

### Automatic strategy regression red

The first exact-margin implementation applied the stricter match globally. The focused route exposed that this changed the legacy automatic strategy contract.

```text
Exit code: 1
Unhandled exception. System.Exception: accepted lifecycle preflight must be ready
   at CAPETF.Desktop.Tests.SyntheticTradingTests.AcceptedBasketSurvivesRestartReconcilesAndClosesWithoutDuplicateMutations()
```

Root cause: automatic test fixtures and the existing automatic flow intentionally match margin legs by epic and side. The stricter identity check is now manual-only; automatic equal-notional behavior remains unchanged.

## Exact Fixture And Results

Representative rules:

```text
ETH: multiplier 9, minimum 0.5, increment 0.1
BTC: multiplier 0.2, minimum 0.1, increment 0.01
```

The smallest shared basket quantity is exactly `q = 0.5`, producing `4.5 ETH` and `0.1 BTC`.

BUY fixture:

```text
ETH offer 2000 -> notional 9000 -> 20% margin 1800
BTC offer 30000 -> notional 3000 -> 50% margin 1500
Total notional 12000; total margin 3300
```

SELL fixture:

```text
ETH bid 1990 -> notional 8955 -> 20% margin 1791
BTC bid 29900 -> notional 2990 -> 50% margin 1495
Total notional 11945; total margin 3286
```

An off-grid `q = 0.06` produces exact raw quantities `0.54 ETH` and `0.012 BTC`. Preflight blocks on the ETH `0.1` grid violation and never changes the quantities to independently rounded values.

## Final Verification

Focused trading command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- trading
```

Result:

```text
Exit code: 0
Wall time: 6.5 seconds
SyntheticTrading tests passed
```

Full test command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Result:

```text
Exit code: 0
Wall time: 39.7 seconds
SyntheticTrading tests passed
SyntheticBasketBuilder tests passed
```

Release build command:

```powershell
dotnet build desktop\CAPETF.Desktop\CAPETF.Desktop.csproj -c Release
```

Result:

```text
Exit code: 0
Build succeeded.
0 Warning(s)
0 Error(s)
```

Whitespace command:

```powershell
git diff --check
```

Result:

```text
Exit code: 0
No whitespace errors. Git reported only LF-to-CRLF working-copy conversion warnings.
```

## Self-Review

- Confirmed `SyntheticOrderSizing.BuildExecutableOrderPreview` branches only when the active basket strategy is `ManualFormula`; all automatic strategies still use `ExecutableLegPreview` and independent equal-notional deal-rule rounding.
- Confirmed manual quantities are direct decimal products of the frozen basket quantity and formula multiplier; the manual branch contains no rounding call.
- Confirmed the shared increment solver uses exact decimal unscaled values, decimal GCD/LCM operations, and loops bounded by the decimal scale and 96-bit representation rather than an unbounded search.
- Confirmed minimum sizes are applied only after the shared ratio-valid increment is established, so increasing to a minimum cannot rebalance one leg.
- Confirmed invalid or absent minimum/increment rules, zero multipliers, decimal overflow/unrepresentable results, below-minimum quantities, and off-grid quantities fail closed with an epic-specific reason.
- Confirmed BUY uses offers and SELL uses bids for positive multipliers; the existing side reversal remains available for signed multipliers.
- Confirmed manual preflight accepts two to four components and blocks mixed/blank currencies, missing prices, stale/future quotes, non-`TRADEABLE` markets, invalid rules, stale/missing/mismatched margin, insufficient funds, and ratio violations.
- Confirmed exact manual margin matching is scoped to manual baskets after the automatic lifecycle regression exposed the scope boundary.
- Confirmed tickets, execution records, the execution ledger, and execution-derived saved basket snapshots round-trip `BasketQuantity`, `9m`, and `0.2m`.
- Confirmed execution-derived manual snapshots reject a leg whose persisted quantity differs from `abs(multiplier * basket quantity)`.
- Confirmed no credential, connection, transport, confirmation, or order-submission method was invoked.

## Commit

- Branch: `feature/cap-com-terminal-v4`
- Base: `27e00ade377c0eb6deabb39ded68dd4d8464407f`
- Subject: `Preserve manual basket execution ratios`
- The final commit hash is returned in the task response because a commit cannot contain its own hash.

## Concerns And Boundaries

- Existing public/browser fields named `basketNotional` and model property `RequestedNotional` remain for compatibility. Manual snapshots now carry an explicit `BasketQuantity`, removing ambiguity in persisted execution state.
- The solver fails closed if an exact shared result exceeds .NET decimal precision/range; it never falls back to binary floating point or approximate rounding.
- Automated fixtures only were used. Capital.com credentials, current live dealing rules, and real account margin were not read or validated, and no order was placed.
