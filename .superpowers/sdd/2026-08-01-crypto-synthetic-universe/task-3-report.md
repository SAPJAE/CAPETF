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

## Fix Round 1

### Scope

Fix base: `d5ac2ac65329a6a0125e8de9b80ead86834d65a4`

Review findings addressed:

1. Moved strategy identity from the current strategy dropdown to the active built/displayed basket.
2. Changed formula resolution to filter the selected crypto block first, then apply exact epic/symbol, exact name, and normalized epic/symbol tiers in that order.
3. Replaced permissive grouped-number parsing with strict invariant decimal grammar and `NumberStyles.AllowDecimalPoint`.
4. Added explicit behavioral coverage for non-crypto components and five-term formulas.

No credentials were read or added. No order was submitted or executed.

### Changed Files

- `desktop/CAPETF.Desktop/ActiveSyntheticBasketState.cs` (new): owns the active basket's strategy identity, saved-basket naming/snapshot, clear behavior, and strategy-aware history rebuild.
- `desktop/CAPETF.Desktop/SyntheticModels.cs`: stores `SyntheticStrategyKind` on `SyntheticBasket`.
- `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`: activates strategy on build/restore, delegates save/name/reload to active basket state, preserves strategy while renaming, and removes save/reload dependence on `SelectedStrategy()`.
- `desktop/CAPETF.Desktop/SavedSyntheticBasketRestorer.cs`: assigns restored strategy identity to the restored basket.
- `desktop/CAPETF.Desktop/ManualSyntheticBasketFactory.cs`: assigns manual strategy identity, implements block-first tiered resolution, and enforces strict invariant decimal syntax.
- `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`: adds behavioral active-state, resolver precedence/ambiguity, strict decimal, non-crypto, and five-term tests; removes the prior source-scan reload assertion.
- `.superpowers/sdd/2026-08-01-crypto-synthetic-universe/task-3-report.md`: appends this Fix Round 1 record.

### Red Evidence

#### Active basket strategy state

Command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- manual-formula
```

Exact observed result:

```text
Exit code: 1
Wall time: 10.9 seconds
SyntheticBasketBuilderTests.cs(7114,25): error CS0246: The type or namespace name 'ActiveSyntheticBasketState' could not be found (are you missing a using directive or an assembly reference?)
The build failed. Fix the build errors and run again.
```

The behavioral test required a state object that did not exist. The implementation now stores strategy on `SyntheticBasket`; the active state uses that identity for save naming, saved snapshots, and manual/automatic history rebuild selection.

#### Selected-block resolver precedence

Command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- manual-formula
```

Exact observed result:

```text
Exit code: 1
Wall time: 10.4 seconds
Unhandled exception. System.InvalidOperationException: Manual formula instrument 'ETHUSD' has a currency outside the selected USD block.
   at CAPETF.Desktop.ManualSyntheticBasketFactory.ResolveTerms(...) in ManualSyntheticBasketFactory.cs:line 194
   at CAPETF.Desktop.Tests.SyntheticBasketBuilderTests.ManualFormulaResolutionIsBlockLocalAndTiered() in SyntheticBasketBuilderTests.cs:line 7086
```

An out-of-block exact symbol suppressed an in-block exact name. Resolution now filters the block first and evaluates each required tier independently; multiple matches at any tier fail as ambiguous.

#### Strict invariant decimal grammar

Command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- manual-formula
```

Exact observed result:

```text
Exit code: 1
Wall time: 10.4 seconds
Unhandled exception. System.Exception: comma decimal separator must throw FormatException
   at CAPETF.Desktop.Tests.SyntheticBasketBuilderTests.AssertThrows[TException](...) in SyntheticBasketBuilderTests.cs:line 7294
   at CAPETF.Desktop.Tests.SyntheticBasketBuilderTests.ManualFormulaResolvesExactIdentifiersAndRejectsInvalidTerms() in SyntheticBasketBuilderTests.cs:line 6985
```

`NumberStyles.Number` accepted `0,2`. The parser now syntax-checks digits with one optional interior dot, rejects comma/group/exponent/explicit-positive-sign forms, handles negative values with the existing positive-only error, and parses only with `NumberStyles.AllowDecimalPoint` plus invariant culture.

#### Intermediate compiler correction

The first strict-parser implementation used the wrong `StartsWith` overload. The focused command produced:

```text
Exit code: 1
Wall time: 3.1 seconds
ManualSyntheticBasketFactory.cs(50,43): error CS1503: Argument 1: cannot convert from 'char' to 'string'
The build failed. Fix the build errors and run again.
```

The call was corrected to the ordinal string overload before the green run.

### Behavioral Coverage Added

- Build a manual basket, activate it as `ManualFormula`, simulate changing the dropdown to `MeanReversion`, then verify active strategy, save strategy, suggested name, history rebuild strategy, and exact `9m`/`0.2m` multipliers remain manual.
- Verify an out-of-block exact symbol cannot suppress an in-block exact name.
- Verify an in-block exact name precedes a normalized in-block epic/symbol fallback.
- Verify duplicate exact names fail at the exact-name tier even when a normalized fallback is unique.
- Verify duplicate normalized identifiers fail at the normalized tier.
- Reject `0,2`, `1,000`, exponent syntax, and explicit positive signs.
- Explicitly reject a five-term formula.
- Explicitly reject a matching non-crypto instrument from a manual crypto formula.

### Final Test Evidence

Focused command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj -- manual-formula
```

Exact output:

```text
Exit code: 0
Wall time: 3.6 seconds
ManualFormula tests passed
```

Full command:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Exact output:

```text
Exit code: 0
Wall time: 39.7 seconds
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

### Self-Review

- Confirmed `SelectedStrategy()` is used only to choose a new build and control editor visibility; save, saved naming, and resolution reload do not read it.
- Confirmed every displayed basket activation path sets strategy: automatic build, manual build, saved restore, and execution restore.
- Confirmed `RenameBasket` copies strategy and clearing `_basket` clears active state.
- Confirmed automatic history rebuild keeps the active automatic strategy while manual rebuild dispatches through exact saved multipliers.
- Confirmed the existing enum member ordering is unchanged; strategy persistence compatibility is retained.
- Confirmed selected-block filtering occurs before all three resolver tiers.
- Confirmed exact epic/symbol, exact name, and normalized epic/symbol each enforce one-success/many-ambiguous semantics.
- Confirmed out-of-block matches are consulted only after all in-block tiers miss and only to produce a clearer rejection.
- Confirmed strict multiplier syntax contains only ASCII digits and one optional interior dot before invariant decimal parsing.
- Confirmed no execution sizing, credential, or order-submission behavior changed.

### Commit

- Branch: `feature/cap-com-terminal-v4`
- Fix base: `d5ac2ac65329a6a0125e8de9b80ead86834d65a4`
- Commit subject: `Fix manual synthetic formula review issues`
- The final Fix Round 1 commit hash is reported in the task response because a commit cannot contain its own final hash.

### Concerns And Boundaries

- Exact ratio-preserving execution remains Task 4 and is intentionally unchanged.
- No credentialed or hands-on Capital.com validation was performed, and no order was placed.
