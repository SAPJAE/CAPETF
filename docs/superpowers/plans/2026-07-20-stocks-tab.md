# Stocks Tab Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a lazy-loaded Stocks tab that uses the same Capital.com chart, performance rank, and investment rank technique as ETFs.

**Architecture:** Keep ETFs as the default dataset and add stocks as a second encrypted dataset. Reuse the existing Capital.com client, pricing, ranking, and card renderer; only market filtering and tab/data-loading logic differ.

**Tech Stack:** Static HTML/CSS/JavaScript, Python 3.12, Capital.com demo API, GitHub Actions, AES-GCM encrypted JSON.

## Global Constraints

- ETF dashboard must load first and must not wait for stocks.
- Stocks should load only when the Stocks tab is clicked.
- Use the same password and encryption flow for ETF and stock data.
- Keep the repo public-safe; no API keys or passwords in files.

---

### Task 1: Generalize Capital.com Instrument Discovery

**Files:**
- Modify: `scripts/update_capital_etfs.py`
- Create: `scripts/update_capital_stocks.py`

**Interfaces:**
- Produces: `discover_instruments(client, kind)` returning sorted market dicts for `etf` or `stock`.
- Produces: `run(output, kind, label)` writing a ranked raw JSON payload.

- [ ] Add stock detection beside existing ETF detection.
- [ ] Replace ETF-only `main()` flow with reusable `run()`.
- [ ] Add a thin stock script that calls `run(..., kind="stock")`.
- [ ] Verify with `python -m py_compile scripts/update_capital_etfs.py scripts/update_capital_stocks.py scripts/encrypt_data.py`.

### Task 2: Lazy-Load Stocks in the Dashboard

**Files:**
- Modify: `index.html`

**Interfaces:**
- Consumes: encrypted datasets at `data/etfs.enc.json` and `data/stocks.enc.json`.
- Produces: tab buttons `ETFs` and `Stocks`; stocks fetch happens only after user selects Stocks.

- [ ] Add tab buttons above controls.
- [ ] Store datasets by key in JavaScript.
- [ ] Unlock ETFs first using `data/etfs.enc.json`.
- [ ] On Stocks tab click, fetch and decrypt `data/stocks.enc.json`, then render existing cards.
- [ ] Show a friendly “not ready yet” message if `stocks.enc.json` is not present before the first workflow run.

### Task 3: Refresh Both Datasets Daily

**Files:**
- Modify: `.github/workflows/refresh.yml`
- Modify: `README.md`

**Interfaces:**
- Produces: `data/etfs.enc.json` and `data/stocks.enc.json`.

- [ ] Run ETF refresh and encryption as before.
- [ ] Run stock refresh and encryption after ETFs.
- [ ] Commit both encrypted files when changed.
- [ ] Update README wording from ETF-only to ETF/stocks.

### Verification

- [ ] `python -m py_compile scripts/update_capital_etfs.py scripts/update_capital_stocks.py scripts/encrypt_data.py`
- [ ] Static grep confirms no secrets are committed.
- [ ] Git diff review confirms ETFs remain default and stocks lazy-load.
