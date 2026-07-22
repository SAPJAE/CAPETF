# CAPETF Realtime Desktop

Local Windows dashboard for Capital.com market data.

## What It Does

- Lets each user enter their own Capital.com identifier, API password, and API key.
- Stores credentials locally with Windows DPAPI for the current Windows user.
- Searches Capital.com instruments with REST API.
- Loads historical chart data for visible instruments.
- Streams realtime quotes for up to 40 visible instruments through Capital.com WebSocket.
- Uses a dense Capital.com-style trade grid with grouped rows, bid/offer, low/high, sparklines, watchlist actions, and alerts.
- Includes workspace modes for Trade, Discover, Charts, Portfolio, Calendar, and Alerts.
- Keeps grouped markets collapsed by default for fast browsing.
- Keeps order-ticket controls in preview mode only. This build does not submit live trades.

## Build

```powershell
.\build-installer.ps1
```

If Inno Setup is installed, the script creates `artifacts/CAPETF-Realtime-Setup.exe`.
Otherwise it creates `artifacts/CAPETF-Realtime-win-x64.zip`.

## Notes

Capital.com limits WebSocket subscriptions to 40 instruments at a time. The app therefore streams visible or selected instruments instead of the full market list.
