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
