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

Historical depth depends on what Capital.com makes available for each epic and resolution. The terminal keeps its requests within those available history limits, so candle count and date range can differ by instrument and timeframe.

The bundled encrypted ETF catalog uses an application-shipped fallback key so the desktop can load its offline universe without user setup. This is data obfuscation, not confidentiality: anyone with the application binaries can recover that fallback key and decrypt the bundled catalog. Capital.com account credentials remain separate and are protected for the current Windows user with DPAPI.

This build is analytical only. It does not submit live orders.
