# cap.com Terminal V4

## Run the unpacked release

Start the self-contained Windows executable directly:

```powershell
.\publish\cap.com-terminal-v4-complete\CAPETF.exe
```

The package includes the .NET 8 Windows Desktop runtime. Microsoft Edge WebView2 Runtime remains an application prerequisite and is normally installed with current Windows releases. Browser state is stored per user under `%LOCALAPPDATA%\CAPETF\WebView2`, outside the unpacked release.

The release stores credentials with Windows DPAPI for the current Windows user. On launch, it uses saved Capital.com credentials when they are available, loads the selected universe, and starts the applicable market-data stream after a basket is built.

## Terminal workflow

1. Choose `Stocks` or `ETFs` from the universe selector. Switching universes reloads the relevant instruments and keeps stock and ETF basket construction separate.
2. Choose a market block and strategy, then build a synthetic basket. The progress panel shows the current loading and selection stage.
3. Use the interval controls and chart tools to inspect the basket. Component legs, formula details, bid/ask, and the selected currency appear with the basket.
4. Save a built basket to restore its formula, components, and workspace state in a later session.
5. In a Capital.com demo session, enter the basket notional and choose `Place Buy Basket` or `Place Sell Basket`. Preflight refreshes every leg, checks current tradeability, quote age, executable size, margin, and available funds.
6. Review the exact side, epic, quantity, and reference price for every leg. The host-issued ticket expires after two minutes and can be used only once.
7. Confirm the demo basket. Legs are submitted sequentially, and each Capital.com deal reference is confirmed before the next leg is submitted.

Historical depth depends on what Capital.com makes available for each epic and resolution. The terminal keeps its requests within those available history limits, so candle count and date range can differ by instrument and timeframe.

The bundled encrypted ETF catalog uses an application-shipped fallback key so the desktop can load its offline universe without user setup. This is data obfuscation, not confidentiality: anyone with the application binaries can recover that fallback key and decrypt the bundled catalog. Capital.com account credentials remain separate and are protected for the current Windows user with DPAPI.

## Demo trading safety

Synthetic basket execution is locked to the Capital.com demo API. A live session cannot place or close a synthetic basket through this terminal. The app permits one trading mutation at a time and does not retry an order mutation whose outcome may be ambiguous.

The main execution states are:

- `Submitting`: the ticket is being processed one leg at a time.
- `Open`: every leg was accepted and has a permanent Capital.com deal ID.
- `Needs attention`: at least one accepted or uncertain leg remains after a rejection, cancellation, timeout, or ambiguous API response. Refresh positions before taking another action.
- `Closing` and `Partially closed`: confirmed open deal IDs are being closed sequentially, or only some closes completed.
- `Closed`: no tracked open leg remains.
- `Rejected`: the attempted basket opened no position and at least one leg was explicitly rejected.

If one leg is accepted and a later leg is rejected, the accepted position remains open. The terminal never submits the remaining legs and never automatically rolls back or closes a partial basket. Use `Close Basket` only after reviewing the exact tracked open deal IDs. An `Unknown` leg is deliberately not retried or blindly closed because the Capital.com mutation may already have succeeded.

## Persistence and recovery

Execution records are written atomically to `%LOCALAPPDATA%\CAPETF\synthetic-executions.json`. The file contains basket state and Capital.com deal references/IDs, not API credentials. On reconnect or restart, the terminal reads this file, compares tracked deal IDs with the demo account's current open positions, saves the reconciled state, and then displays it. A malformed state file is quarantined beside the original file with a `.corrupt-*` suffix.

This build can submit and close demo positions only. It does not submit live-account orders.
