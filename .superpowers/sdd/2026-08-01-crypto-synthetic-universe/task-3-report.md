# Task 3 Report: Manual Synthetic Formula Construction

## Status

Implemented Task 3 only. The manual formula path builds the exact editable preset:

```text
9 ETHUSD + 0.2 BTCUSD
```

It branches before automatic candidate ranking and equal-notional basket selection. It resolves two to four crypto legs within the selected currency block, preserves explicit decimal multipliers, constructs direct-scale candles from strict shared timestamps, restores manual baskets without equal-notional rewriting, and exposes a compact conditional editor.

No credentials were read or added. No order was submitted or executed.

## Changed Files

- `desktop/CAPETF.Desktop/ManualSyntheticBasketFactory.cs` (new): manual formula parser, formatter, robust resolver, validator, direct candle factory, and manual restore implementation.
- `desktop/CAPETF.Desktop/SyntheticStrategy.cs`: appended `SyntheticStrategyKind.ManualFormula` and added the catalog label without renumbering existing enum values.
- `desktop/CAPETF.Desktop/SyntheticModels.cs`: added signed execution-side Bid/Ask aggregation.
- `desktop/CAPETF.Desktop/SyntheticHistoryService.cs`: exposed the existing alignment-key function internally for strict manual candle intersection.
- `desktop/CAPETF.Desktop/SavedSyntheticBasketStore.cs`: added a formula-bearing display label while retaining exact decimal component persistence.
- `desktop/CAPETF.Desktop/SavedSyntheticBasketRestorer.cs`: accepts two-to-four-leg manual baskets and dispatches them to the manual restore path.
- `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`: added the compact, editable, initially collapsed formula input.
- `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`: added the manual-only build branch, conditional editor behavior, formula restore, direct manual history build, and multiplier-preserving timeframe reload.
- `desktop/CAPETF.Desktop.Tests/Program.cs`: added the `manual-formula` focused test filter while preserving existing overloads and filters.
- `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`: added focused parser, resolution, validation, candle, quote, persistence, restore, reload, and UI workflow coverage.
- `.superpowers/sdd/2026-08-01-crypto-synthetic-universe/task-3-report.md`: this report.

## TDD Red Evidence

### Initial manual architecture red

Command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- manual-formula
```

Observed output:

```text
Exit code: 1
SyntheticBasketBuilderTests.cs(6889,23): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(6905,22): error CS0103: The name 'ManualSyntheticBasketFactory' does not exist in the current context
SyntheticBasketBuilderTests.cs(6943,26): error CS0103: The name 'ManualSyntheticBasketFactory' does not exist in the current context
SyntheticBasketBuilderTests.cs(6946,13): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(6952,19): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(6956,19): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(6960,19): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(6964,19): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(6969,19): error CS0103: The name 'ManualSyntheticBasketFactory' does not exist in the current context
SyntheticBasketBuilderTests.cs(6972,17): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(6978,19): error CS0103: The name 'ManualSyntheticBasketFactory' does not exist in the current context
SyntheticBasketBuilderTests.cs(6981,17): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(6990,19): error CS0103: The name 'ManualSyntheticBasketFactory' does not exist in the current context
SyntheticBasketBuilderTests.cs(6993,17): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(7001,19): error CS0103: The name 'ManualSyntheticBasketFactory' does not exist in the current context
SyntheticBasketBuilderTests.cs(7004,17): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(7032,26): error CS0103: The name 'ManualSyntheticBasketFactory' does not exist in the current context
SyntheticBasketBuilderTests.cs(7035,17): error CS0103: The name 'ManualSyntheticFormula' does not exist in the current context
SyntheticBasketBuilderTests.cs(7039,95): error CS0117: 'SyntheticStrategyKind' does not contain a definition for 'ManualFormula'
SyntheticBasketBuilderTests.cs(7042,47): error CS0117: 'SyntheticStrategyKind' does not contain a definition for 'ManualFormula'
SyntheticBasketBuilderTests.cs(7049,47): error CS0117: 'SyntheticStrategyKind' does not contain a definition for 'ManualFormula'
The build failed. Fix the build errors and run again.
```

The failures were caused by the intentionally absent parser, factory, and strategy kind.

### Timeframe reload regression red

After the initial green implementation, self-review identified that the existing resolution reload rebuilt every basket through the equal-notional path. A test was added before the fix.

Command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- manual-formula
```

Observed output:

```text
Exit code: 1
Unhandled exception. System.Exception: manual timeframe reload must preserve exact formula identity: missing SyntheticStrategyKind.ManualFormula
   at CAPETF.Desktop.Tests.SyntheticBasketBuilderTests.ManualFormulaEditorIsCompactConditionalAndBypassesAutomaticSelection() in SyntheticBasketBuilderTests.cs:line 7135
```

The reload path was then split so manual baskets restore their saved component multipliers and direct-scale candles, while automatic strategies retain `SyntheticHistoryService.BuildSelected`.

### Separator-normalized symbol red

Self-review also added a realistic Capital symbol case where the API symbol is `ETH/USD` or `BTC/USD` while the required preset tokens are `ETHUSD` and `BTCUSD`.

Command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- manual-formula
```

Observed output:

```text
Exit code: 1
Unhandled exception. System.InvalidOperationException: Manual formula instrument 'ETHUSD' was not found.
   at CAPETF.Desktop.ManualSyntheticBasketFactory.ResolveTerms(...) in ManualSyntheticBasketFactory.cs:line 173
   at CAPETF.Desktop.ManualSyntheticBasketFactory.Create(...) in ManualSyntheticBasketFactory.cs:line 89
```

A conservative alphanumeric epic/symbol fallback was added after exact epic/symbol matching and before exact name matching. Multiple normalized matches still fail as ambiguous.

## Final Test Evidence

Focused command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- manual-formula
```

Exact output:

```text
Exit code: 0
Wall time: 3.5 seconds
ManualFormula tests passed
```

Full command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Exact output:

```text
Exit code: 0
Wall time: 39.4 seconds
SyntheticTrading tests passed
SyntheticBasketBuilder tests passed
```

Whitespace command:

```powershell
git diff --check
```

Exact result:

```text
Exit code: 0
Wall time: 0.5 seconds
No whitespace errors. Git emitted LF-to-CRLF working-copy conversion warnings for modified tracked files.
```

## Self-Review

- Confirmed the manual branch occurs before `SyntheticTerminalSelector.HistoryLoadCandidates`, candidate fallback loading, ranking, and `SyntheticTerminalSelector.SelectBest`.
- Confirmed the factory copies parsed decimal multipliers directly into `SyntheticComponent.FormulaMultiplier`; it never calls equal-weight or display-multiplier calculation.
- Confirmed the exact preset produces two legs in ETH then BTC order with multipliers `9m` and `0.2m`.
- Confirmed direct OHLC math uses only shared timestamp/alignment keys and does not call `NormalizeSyntheticCandleOpens` or rebase the first candle to 100.
- Confirmed exact epic/symbol matches take priority, separator-normalized epic/symbol matches are a bounded fallback, exact names are supported, and every ambiguous result fails closed.
- Confirmed unknown instruments, duplicate resolved epics, non-Crypto blocks, mixed currencies, malformed terms, one/five-term formulas, and zero/negative multipliers are rejected with actionable errors.
- Confirmed signed Bid/Ask semantics select offer for a negative leg on synthetic Bid and bid for a negative leg on synthetic Ask.
- Confirmed saved JSON retains decimal `FormulaMultiplier` values and the manual strategy; restore accepts exactly two to four manual legs and uses the direct factory.
- Confirmed timeframe changes preserve manual multipliers instead of rebuilding through equal-notional selection.
- Confirmed `ManualFormula` was appended to the enum, preserving numeric values of existing saved strategies.
- Confirmed the input is compact, editable, defaults to the exact preset, appears only in manual mode, and the seed input is hidden in that mode.
- Confirmed no credential handling or order-execution code was added or invoked.

## Commit

- Branch: `feature/cap-com-terminal-v4`
- Commit subject: `Add manual crypto synthetic formulas`
- The implementation, tests, and this report are committed together; the final commit hash is reported in the task response because a commit cannot contain its own final hash.

## Concerns And Boundaries

- Exact ratio-preserving Capital deal-rule sizing and manual basket quantity execution belong to Task 4 and were intentionally not implemented here. Existing automatic-strategy order sizing remains unchanged.
- No credentialed or hands-on Capital.com validation was performed, and no order was placed. Task 3 validation is automated only.
- Negative parsed multipliers are rejected because the Task 3 brief explicitly requires negative legs to fail. Signed quote math is nevertheless correct for any existing signed component model.
