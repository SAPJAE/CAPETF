# Quality Dip Rank Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Capital.com-only stock ranking that finds smooth long-term uptrends trading at a meaningful discount with evidence of stabilization.

**Architecture:** Put the statistical scoring in a focused Python module that consumes normalized Capital.com daily price rows and returns serializable metrics. Add those metrics to each stock during batch generation, then calculate the global display rank in the browser after all available batches are merged.

**Tech Stack:** Python 3.12 standard library, `unittest`, existing Capital.com REST pipeline, static HTML/CSS/JavaScript dashboard.

## Global Constraints

- Apply Quality Dip Rank to stocks only.
- Keep ETF, Performance Rank, and Investment Rank behavior unchanged.
- Use only Capital.com instrument metadata and historical prices.
- Do not use external fundamentals, websites, or additional API keys.
- Missing or insufficient history must display `Unrated`, never score as zero.
- A final global rank is calculated only across the merged stock dataset.
- Store scoring version `quality-dip-v1` in generated stock records.

---

### Task 1: Price-pattern scoring module

**Files:**
- Create: `scripts/quality_dip.py`
- Create: `tests/test_quality_dip.py`

**Interfaces:**
- Consumes: daily rows shaped as `{"date": "YYYY-MM-DD", "close": float, "high": float, "low": float}` and optional `bid`/`offer` floats.
- Produces: `quality_dip_metrics(rows: list[dict], bid: float | None = None, offer: float | None = None) -> dict`.
- Produces fields: `qualityDipScore`, `qualityDipLabel`, `qualityDipTrendScore`, `qualityDipDiscountScore`, `qualityDipStabilizationScore`, `qualityDipRiskScore`, `qualityDipDrawdownPct`, `qualityDipTrendDistancePct`, `qualityDipRank`, and `qualityDipVersion`.

- [ ] **Step 1: Write deterministic fixtures and failing eligibility tests**

```python
import math
import unittest
from datetime import date, timedelta

from scripts.quality_dip import quality_dip_metrics


def weekly_rows(values):
    start = date(2021, 1, 1)
    return [
        {
            "date": (start + timedelta(days=index * 7)).isoformat(),
            "close": float(value),
            "high": float(value) * 1.01,
            "low": float(value) * 0.99,
        }
        for index, value in enumerate(values)
    ]


class QualityDipTests(unittest.TestCase):
    def test_insufficient_history_is_unrated(self):
        result = quality_dip_metrics(weekly_rows(range(100, 200)))
        self.assertIsNone(result["qualityDipScore"])
        self.assertEqual(result["qualityDipLabel"], "Unrated")
        self.assertEqual(result["qualityDipVersion"], "quality-dip-v1")
```

- [ ] **Step 2: Run the eligibility test and verify it fails**

Run: `python -m unittest tests.test_quality_dip.QualityDipTests.test_insufficient_history_is_unrated -v`

Expected: `ModuleNotFoundError: No module named 'scripts.quality_dip'`.

- [ ] **Step 3: Implement weekly normalization and the unrated result**

```python
SCORING_VERSION = "quality-dip-v1"
MIN_WEEKLY_POINTS = 120
MIN_HISTORY_DAYS = 365 * 3


def _weekly_rows(rows):
    selected = {}
    for row in rows:
        if not row.get("date") or not row.get("close") or row["close"] <= 0:
            continue
        day = date.fromisoformat(row["date"])
        year, week, _ = day.isocalendar()
        selected[(year, week)] = row
    return [selected[key] for key in sorted(selected)]


def _unrated():
    return {
        "qualityDipScore": None,
        "qualityDipLabel": "Unrated",
        "qualityDipTrendScore": None,
        "qualityDipDiscountScore": None,
        "qualityDipStabilizationScore": None,
        "qualityDipRiskScore": None,
        "qualityDipDrawdownPct": None,
        "qualityDipTrendDistancePct": None,
        "qualityDipRank": None,
        "qualityDipVersion": SCORING_VERSION,
    }


def quality_dip_metrics(rows, bid=None, offer=None):
    weekly = _weekly_rows(rows)
    history_days = (
        date.fromisoformat(weekly[-1]["date"]) - date.fromisoformat(weekly[0]["date"])
    ).days if weekly else 0
    if len(weekly) < MIN_WEEKLY_POINTS or history_days < MIN_HISTORY_DAYS:
        return _unrated()
```

- [ ] **Step 4: Add failing behavioral tests**

Create fixtures with 260 weekly values:

```python
def smooth_uptrend(count=260):
    return [100 * math.exp(0.0045 * index) for index in range(count)]


def quality_dip():
    values = smooth_uptrend()
    peak = values[-30]
    values[-30:] = [peak * (1 - 0.012 * index) for index in range(30)]
    values[-5:] = [values[-6] * factor for factor in (0.99, 0.995, 1.0, 1.015, 1.03)]
    return values


def persistent_collapse(count=260):
    return [220 * math.exp(-0.006 * index) for index in range(count)]


def test_quality_dip_beats_near_high_and_collapse(self):
    dip = quality_dip_metrics(weekly_rows(quality_dip()))
    near_high = quality_dip_metrics(weekly_rows(smooth_uptrend()))
    collapse = quality_dip_metrics(weekly_rows(persistent_collapse()))
    self.assertGreater(dip["qualityDipScore"], near_high["qualityDipScore"])
    self.assertGreater(dip["qualityDipScore"], collapse["qualityDipScore"])


def test_extreme_collapse_is_capped(self):
    values = smooth_uptrend(220) + [350 * (0.92 ** index) for index in range(40)]
    result = quality_dip_metrics(weekly_rows(values))
    self.assertLessEqual(result["qualityDipScore"], 55)


def test_result_is_deterministic_and_bounded(self):
    first = quality_dip_metrics(weekly_rows(quality_dip()), 100, 100.2)
    second = quality_dip_metrics(weekly_rows(quality_dip()), 100, 100.2)
    self.assertEqual(first, second)
    self.assertGreaterEqual(first["qualityDipScore"], 0)
    self.assertLessEqual(first["qualityDipScore"], 100)
```

- [ ] **Step 5: Run behavioral tests and verify they fail**

Run: `python -m unittest tests.test_quality_dip -v`

Expected: failures because the eligible scoring path returns no result.

- [ ] **Step 6: Implement scoring helpers and component formulas**

Implement:

```python
def _clamp(value, low, high):
    return max(low, min(high, value))


def _scale(value, low, high, points):
    return _clamp((value - low) / (high - low), 0, 1) * points


def _mean(values):
    return sum(values) / len(values)


def _linear_regression(values):
    xs = list(range(len(values)))
    x_mean = _mean(xs)
    y_mean = _mean(values)
    denominator = sum((x - x_mean) ** 2 for x in xs)
    slope = sum((x - x_mean) * (y - y_mean) for x, y in zip(xs, values)) / denominator
    intercept = y_mean - slope * x_mean
    fitted = [intercept + slope * x for x in xs]
    total = sum((value - y_mean) ** 2 for value in values)
    residual = sum((value - estimate) ** 2 for value, estimate in zip(values, fitted))
    r_squared = 1 - residual / total if total else 1
    return slope, intercept, _clamp(r_squared, 0, 1)
```

Calculate:

- `trend` out of 40 from annualized log-regression return (12), positive calendar-year ratio (10), positive regression slope (10), and `R-squared` (8).
- `discount` out of 30 from 52-week drawdown (12), fitted-trend distance (10), and previous-calendar-year-low proximity (8).
- `stabilization` out of 20 from no fresh 13-week low (6), improving four-week return (5), proximity/reclaim of 10-week average (5), and non-negative recent 10-week-average slope (4).
- `risk` out of 10 from current-versus-historical drawdown (4), recent-versus-historical weekly volatility (3), and optional bid/offer spread (3). Normalize risk to 10 when spread is unavailable.
- Apply the approved trend, falling-average, fresh-low, and 70%-drawdown gates.
- Round component values and percentages to two decimals.
- Assign labels at 80, 65, and 50; require trend `>= 28` and stabilization `>= 14` for `Confirmed quality dip`.

- [ ] **Step 7: Run scorer tests**

Run: `python -m unittest tests.test_quality_dip -v`

Expected: all tests pass.

- [ ] **Step 8: Commit scorer**

```powershell
git add scripts/quality_dip.py tests/test_quality_dip.py
git commit -m "Add price-based quality dip scorer"
```

---

### Task 2: Stock pipeline integration

**Files:**
- Modify: `scripts/update_capital_etfs.py`
- Modify: `scripts/update_capital_stocks.py`
- Create: `tests/test_stock_quality_dip.py`

**Interfaces:**
- Consumes: `quality_dip_metrics(rows, bid, offer)` from Task 1.
- Produces: quality-dip fields on stock records only.
- Produces: `qualityDipPartialRank` inside individual generated batch payloads; the browser does not present it as the final global rank.

- [ ] **Step 1: Write failing stock-only integration tests**

Use `unittest.mock.patch` and generated 260-week fixtures:

```python
from scripts.update_capital_etfs import build_item


class StockPipelineQualityDipTests(unittest.TestCase):
    def test_stock_item_contains_quality_dip_metrics(self):
        market = {
            "epic": "TEST",
            "instrumentName": "Test Stock",
            "instrumentType": "SHARES",
            "bid": 100,
            "offer": 100.2,
        }
        item = build_item(market, weekly_rows(quality_dip()), kind="stock")
        self.assertIsNotNone(item["qualityDipScore"])
        self.assertEqual(item["qualityDipVersion"], "quality-dip-v1")

    def test_etf_item_does_not_contain_quality_dip_metrics(self):
        market = {"epic": "ETF", "instrumentName": "Test ETF", "instrumentType": "ETF"}
        item = build_item(market, weekly_rows(quality_dip()), kind="etf")
        self.assertNotIn("qualityDipScore", item)
```

- [ ] **Step 2: Run integration tests and verify failure**

Run: `python -m unittest tests.test_stock_quality_dip -v`

Expected: failure because `build_item` has no `kind` parameter.

- [ ] **Step 3: Pass dataset kind through the generation pipeline**

Change the signature to:

```python
def build_item(market, rows, hourly_rows=None, kind=None):
```

Call `quality_dip_metrics` only when `kind == "stock"`:

```python
if kind == "stock":
    item.update(
        quality_dip_metrics(
            rows,
            market.get("bid"),
            market.get("offer"),
        )
    )
```

Update all ETF calls to pass `kind="etf"` and all stock calls in
`scripts/update_capital_stocks.py` and `run()` to pass `kind="stock"`.

- [ ] **Step 4: Add failing partial-rank tests**

```python
def test_classify_assigns_only_partial_quality_rank(self):
    items = [
        {"name": "B", "validated": True, "qualityDipScore": 60, "returnTotal": 1},
        {"name": "A", "validated": True, "qualityDipScore": 80, "returnTotal": 2},
        {"name": "U", "validated": True, "qualityDipScore": None, "returnTotal": 3},
    ]
    payload = classify(items, {"chunkIndex": 0, "chunkCount": 11})
    by_name = {item["name"]: item for item in payload["items"]}
    self.assertEqual(by_name["A"]["qualityDipPartialRank"], 1)
    self.assertEqual(by_name["B"]["qualityDipPartialRank"], 2)
    self.assertIsNone(by_name["U"].get("qualityDipPartialRank"))
```

- [ ] **Step 5: Implement deterministic partial ranking**

In `classify()` sort scored items by descending score and then name:

```python
quality_dips = sorted(
    [item for item in items if item.get("qualityDipScore") is not None],
    key=lambda item: (-item["qualityDipScore"], item["name"]),
)
for index, item in enumerate(quality_dips, start=1):
    item["qualityDipPartialRank"] = index
```

Add `qualityDipScoringVersion: "quality-dip-v1"` to stock batch metadata.

- [ ] **Step 6: Run all Python tests**

Run: `python -m unittest discover -s tests -v`

Expected: all tests pass.

- [ ] **Step 7: Commit pipeline integration**

```powershell
git add scripts/update_capital_etfs.py scripts/update_capital_stocks.py tests/test_stock_quality_dip.py
git commit -m "Add quality dip metrics to stock data"
```

---

### Task 3: Dashboard rank, sorts, and tile metrics

**Files:**
- Modify: `index.html`
- Create: `tests/dashboard_quality_dip.test.js`

**Interfaces:**
- Consumes: generated quality-dip fields from Task 2.
- Produces: `displayQualityDipRank` over all currently merged scored stocks.
- Produces sort values `qualityDip`, `qualityDiscount`, and `qualityStabilizing`.

- [ ] **Step 1: Extract and test pure ranking behavior**

Add browser-callable pure helpers to `index.html`:

```javascript
function compareQualityDip(a, b) {
  const aScore = Number.isFinite(a.qualityDipScore) ? a.qualityDipScore : -Infinity;
  const bScore = Number.isFinite(b.qualityDipScore) ? b.qualityDipScore : -Infinity;
  return bScore - aScore || a.name.localeCompare(b.name);
}

function assignQualityDipRanks(items, complete) {
  items.forEach(item => { delete item.displayQualityDipRank; });
  if (!complete) return;
  items
    .filter(item => Number.isFinite(item.qualityDipScore))
    .sort(compareQualityDip)
    .forEach((item, index) => { item.displayQualityDipRank = index + 1; });
}
```

In `tests/dashboard_quality_dip.test.js`, load these functions through Node's
`vm` module and assert:

```javascript
assert.deepEqual(
  items.sort(compareQualityDip).map(item => item.name),
  ["Highest", "Tie A", "Tie B", "Unrated"]
);
assignQualityDipRanks(items, false);
assert.equal(items.some(item => item.displayQualityDipRank), false);
assignQualityDipRanks(items, true);
assert.equal(items.find(item => item.name === "Highest").displayQualityDipRank, 1);
assert.equal(items.find(item => item.name === "Unrated").displayQualityDipRank, undefined);
```

- [ ] **Step 2: Run JavaScript test and verify failure**

Run: `node tests/dashboard_quality_dip.test.js`

Expected: failure because the helpers do not exist.

- [ ] **Step 3: Implement global rank completeness**

In `prepareDisplayRanks()`, consider the stock load complete only when:

```javascript
const complete = activeDataset === 'stocks'
  && Number(payload.summary?.chunkCount) >= Number(payload.summary?.totalChunks);
assignQualityDipRanks(items, complete);
```

Do not expose `qualityDipPartialRank` as a global rank.

- [ ] **Step 4: Add the three stock sort options**

Add:

```html
<option value="qualityDip">Quality dip, best to worst</option>
<option value="qualityDiscount">Largest discount in quality trend</option>
<option value="qualityStabilizing">Stabilizing dips</option>
```

Hide or disable these choices outside the Stocks tab using the existing dataset
switch/render path.

Implement deterministic comparisons:

```javascript
if (sort.value === 'qualityDip') return compareQualityDip(a, b);
if (sort.value === 'qualityDiscount') {
  return (a.qualityDipTrendDistancePct ?? 999999)
    - (b.qualityDipTrendDistancePct ?? 999999)
    || compareQualityDip(a, b);
}
if (sort.value === 'qualityStabilizing') {
  return (b.qualityDipStabilizationScore ?? -999999)
    - (a.qualityDipStabilizationScore ?? -999999)
    || compareQualityDip(a, b);
}
```

- [ ] **Step 5: Add compact tile metrics**

For stock cards only, render:

```html
<div class="quality-dip">
  <div><span>Quality dip</span><strong>rank or Pending</strong></div>
  <div><span>Score</span><strong>score or Unrated</strong></div>
  <div><span>Signal</span><strong>label</strong></div>
  <div><span>Trend</span><strong>trend/40</strong></div>
  <div><span>Discount</span><strong>discount/30</strong></div>
  <div><span>Stabilizing</span><strong>stabilization/20</strong></div>
</div>
<div class="code">52W drawdown ... · vs trend ...</div>
```

Use the existing compact metric typography and responsive grid. Do not add a
nested card or increase the sticky header height.

- [ ] **Step 6: Run dashboard and Python tests**

Run:

```powershell
node tests/dashboard_quality_dip.test.js
python -m unittest discover -s tests -v
```

Expected: all tests pass.

- [ ] **Step 7: Perform local browser verification**

Start:

```powershell
python -m http.server 8765
```

Verify at `http://localhost:8765/`:

- ETFs do not show Quality Dip fields.
- Stocks show score and component details.
- Unrated stocks sort after scored stocks.
- Rank says `Pending` while batches are incomplete.
- All three new sorts produce visibly different, sensible orders.
- Mobile width has no clipped text or overlapping controls.

- [ ] **Step 8: Commit dashboard changes**

```powershell
git add index.html tests/dashboard_quality_dip.test.js
git commit -m "Show quality dip ranking in stock dashboard"
```

---

### Task 4: Final verification and publication

**Files:**
- Verify: `scripts/quality_dip.py`
- Verify: `scripts/update_capital_etfs.py`
- Verify: `scripts/update_capital_stocks.py`
- Verify: `index.html`

**Interfaces:**
- Confirms the scorer, generator, merged ranking, and browser display work as one feature.

- [ ] **Step 1: Run the complete local verification suite**

Run:

```powershell
python -m unittest discover -s tests -v
node tests/dashboard_quality_dip.test.js
dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release
git diff --check
```

Expected: every command exits with code 0.

- [ ] **Step 2: Generate one local sample stock record without committing credentials**

Use the existing Capital.com environment variables to run a one-stock sample
into `artifacts/quality-dip-sample.raw.json`. Confirm it contains
`qualityDipVersion: quality-dip-v1`, all component scores, and no API key or
credentials.

- [ ] **Step 3: Inspect the final diff**

Run:

```powershell
git status --short
git diff --stat HEAD~3
git log -4 --oneline
```

Expected: only scorer, tests, stock integration, dashboard, and design/plan
files are present.

- [ ] **Step 4: Push the commits**

Run: `git push origin main`

Expected: Git reports that `main` was updated successfully.

- [ ] **Step 5: Confirm GitHub Actions begins the refresh**

Confirm the `Refresh market data` workflow starts from the push. The published
dashboard will show the new fields after refreshed stock batches containing
`quality-dip-v1` are available.
