# Synthetic Margin Preview Final Fix Report

## Status

Stopped immediately at the user's request. The implementation and automated test fixes are present in the worktree but are **uncommitted**. The final self-contained publish command was interrupted after 2.6 seconds, so the publish directory may be partial and its asset hashes/no-zip state are not verified.

No GUI verification was started in this wave. The last explicit `Get-Process CAPETF` check returned no process rows (exit code 1), so no CAPETF GUI process was observed running.

## Implemented Changes

- Made blank API-fallback instrument currency enrichable from market details in both the margin service and window detail merge.
- Preserved expired cached account availability when refresh fails; summaries now serialize `IsAccountStale` and `AccountError`.
- Added a five-second failed-FX cache while retaining the existing 30-second successful conversion cache.
- Centralized margin request cancellation/ownership release and WebView reset for clear, failed restore, and successful login.
- Rejected blank, nonpositive, and non-decimal WebView notionals with `Enter a basket notional greater than 0.`; removed the host's silent 300 fallback.
- Rendered effective side, quantity, execution price, native notional, native margin, and account-currency margin for each leg.
- Restored the prior terminal status when the margin refreshing busy owner completes.
- Added null margin-factor, nonpositive conversion-rate, and null/zero default-lot tests.
- Added Node-executed inline terminal DOM coverage for complete, unavailable, negative, stale, clear/reset, detailed legs, invalid input, busy completion, and burst debounce behavior without new dependencies.
- Added no live-order endpoint or submission path.

## TDD Evidence

Initial baseline:

```text
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -c Release
Exit 0, 49.6s: SyntheticBasketBuilder tests passed
```

Observed RED failures before their production fixes:

```text
Blank currency: Synthetic basket currency is unavailable or inconsistent.
Expired account refresh: InvalidOperationException: account refresh failed
Failed FX cache: Expected 2 searches, got 4
Context reset/input: missing ResetMarginPreviewContextAsync contract
```

Final automated suite evidence:

```text
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -c Release
Exit 0, 39.2s: SyntheticBasketBuilder tests passed
```

An intermediate full run also exposed and corrected an obsolete source-contract assertion for the renamed async login reset; this was a test compatibility issue, not a product failure.

## Build And Publish

Test-project Release build:

```text
dotnet build desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -c Release --no-restore
Exit 0: 0 warnings, 0 errors (3.82s)
```

Desktop Release build:

```text
dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release --no-restore
Exit 0: 0 warnings, 0 errors (1.32s)
```

Publish attempt:

```text
dotnet publish desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release -r win-x64 --self-contained true -o desktop/publish/cap.com-terminal-v4-complete
Interrupted by user after 2.6s; completion not established.
```

The requested published HTML/source hash comparison and no-zip verification were not run after the interrupted publish.

## Commits

None. All final-wave changes, including this report, are uncommitted.

## Changed Files

- `desktop/CAPETF.Desktop/Models.cs`
- `desktop/CAPETF.Desktop/SyntheticMarginCalculator.cs`
- `desktop/CAPETF.Desktop/SyntheticMarginPreviewService.cs`
- `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`
- `.superpowers/sdd/2026-07-28-synthetic-margin-preview/final-fix-report.md`

## Residual Concerns

- The self-contained publish may be incomplete because it was interrupted; do not treat `desktop/publish/cap.com-terminal-v4-complete` as release-ready from this evidence.
- Published asset hashes and archive absence remain unverified.
- The final working-tree diff was not given a last review pass and no commit was created before the stop instruction.
- Manual Capital.com demo verification remains intentionally unperformed under the bounded/no-GUI instruction.

## Mechanical Closeout

This section supersedes the earlier stopped/uncommitted packaging status above.

### Diff And Scope Review

```text
git diff --check
Exit 0; no whitespace errors. Git emitted only the repository's existing LF-to-CRLF warnings.
```

The final diff was inspected before commit and mapped to all requested findings:

- Blank API-fallback currency enrichment from market details.
- Stale retention of the last successful account availability after refresh failure.
- Ownership cancellation and immediate WebView reset on clear, failed restore, and login reset.
- Detailed effective side, quantity, execution price, notional, and margin-contribution rows.
- Explicit unavailable input handling for blank/nonpositive/invalid notionals with no 300 fallback.
- Refresh-label completion, short failed-FX caching, arithmetic edge tests, and Node-executed WebView behavior coverage.
- No live-order endpoint or submission path was added.

Source and test changes were committed as:

```text
902b8d9 fix: complete synthetic margin preview safeguards
6 files changed, 632 insertions(+), 51 deletions(-)
```

Per the mechanical-closeout instruction, tests and build were not rerun. Their passing evidence remains recorded earlier in this report.

### Publish Evidence

```text
dotnet publish desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release -r win-x64 --self-contained true -o desktop/publish/cap.com-terminal-v4-complete
Exit 0, 4.8s
All projects were up-to-date for restore.
Published to desktop/publish/cap.com-terminal-v4-complete/
```

Artifact verification:

```text
Source SHA256:    8FCB6C15B3C192543E0555BD50BD304C49693AB7552FA620CA15F020EB042A76
Published SHA256: 8FCB6C15B3C192543E0555BD50BD304C49693AB7552FA620CA15F020EB042A76
Hashes match:     true
CAPETF.exe exists: true
Zip count:         0
Published files:   494
```

No GUI was launched during closeout.

### Closeout Residual Concerns

- Manual Capital.com demo/API verification remains outside this mechanical closeout.
- The unzipped publish directory is verified locally but is ignored by Git and is not part of the source commit.
