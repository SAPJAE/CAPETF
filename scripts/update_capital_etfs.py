import argparse
import json
import math
import os
import time
import urllib.error
import urllib.parse
import urllib.request
from datetime import date, datetime, timezone, timedelta
from pathlib import Path

try:
    from stock_classification import enrich_classification, region_for
except ImportError:
    from scripts.stock_classification import enrich_classification, region_for

try:
    from quality_dip import VERSION as QUALITY_DIP_SCORING_VERSION, quality_dip_metrics
except ImportError:
    from scripts.quality_dip import VERSION as QUALITY_DIP_SCORING_VERSION, quality_dip_metrics


DEMO_BASE_URL = "https://demo-api-capital.backend-capital.com/api/v1"
LIVE_BASE_URL = "https://api-capital.backend-capital.com/api/v1"
ALWAYS_INCLUDE_STOCK_TERMS = ("sap",)
SECTOR_NAMES = {
    "basic materials": "Materials",
    "communication services": "Communication Services",
    "communications": "Communication Services",
    "consumer cyclical": "Consumer Discretionary",
    "consumer defensive": "Consumer Staples",
    "consumer discretionary": "Consumer Discretionary",
    "consumer goods": "Consumer Staples",
    "consumer services": "Consumer Discretionary",
    "consumer staples": "Consumer Staples",
    "energy": "Energy",
    "financial": "Financials",
    "financial services": "Financials",
    "financials": "Financials",
    "health care": "Healthcare",
    "healthcare": "Healthcare",
    "industrial goods": "Industrials",
    "industrials": "Industrials",
    "information technology": "Technology",
    "materials": "Materials",
    "real estate": "Real Estate",
    "technology": "Technology",
    "telecommunications": "Communication Services",
    "utilities": "Utilities",
}


class CapitalClient:
    def __init__(self):
        use_demo = os.environ.get("CAPITAL_DEMO", "true").lower() != "false"
        self.base_url = DEMO_BASE_URL if use_demo else LIVE_BASE_URL
        self.api_key = required_env("CAPITAL_API_KEY")
        self.identifier = required_env("CAPITAL_IDENTIFIER")
        self.password = required_env("CAPITAL_PASSWORD")
        self.cst = ""
        self.security_token = ""

    def request(self, method, path, body=None, auth=True):
        data = json.dumps(body).encode("utf-8") if body is not None else None
        req = urllib.request.Request(self.base_url + path, data=data, method=method)
        req.add_header("X-CAP-API-KEY", self.api_key)
        req.add_header("Content-Type", "application/json")
        if auth:
            req.add_header("CST", self.cst)
            req.add_header("X-SECURITY-TOKEN", self.security_token)
        for attempt in range(6):
            try:
                with urllib.request.urlopen(req, timeout=45) as response:
                    text = response.read().decode("utf-8")
                    if path == "/session":
                        self.cst = response.headers.get("CST", "")
                        self.security_token = response.headers.get("X-SECURITY-TOKEN", "")
                    return json.loads(text) if text else {}
            except urllib.error.HTTPError as exc:
                if exc.code not in (429, 500, 502, 503, 504) or attempt == 5:
                    raise
                retry_after = exc.headers.get("Retry-After")
                delay = int(retry_after) if retry_after and retry_after.isdigit() else min(90, 8 * (attempt + 1))
                print(f"{method} {path} returned HTTP {exc.code}; retrying in {delay}s.", flush=True)
                time.sleep(delay)

    def login(self):
        self.request(
            "POST",
            "/session",
            {
                "identifier": self.identifier,
                "password": self.password,
                "encryptedPassword": False,
            },
            auth=False,
        )
        if not self.cst or not self.security_token:
            raise RuntimeError("Capital.com session did not return CST and X-SECURITY-TOKEN.")

    def get(self, path):
        return self.request("GET", path)


def required_env(name):
    value = os.environ.get(name)
    if not value:
        raise SystemExit(f"{name} environment variable is required.")
    return value


def extract_markets(payload):
    markets = []
    stack = [payload]
    while stack:
        item = stack.pop()
        if isinstance(item, dict):
            if item.get("epic"):
                markets.append(item)
            stack.extend(item.values())
        elif isinstance(item, list):
            stack.extend(item)
    deduped = {}
    for market in markets:
        deduped[market["epic"]] = market
    return list(deduped.values())


def extract_navigation_markets(payload, path):
    markets = []
    for market in payload.get("markets", []) or []:
        enriched = dict(market)
        enriched["navigationPath"] = path
        markets.append(enriched)
    return markets


def market_text(market):
    parts = []
    for key in (
        "instrumentName",
        "epic",
        "symbol",
        "instrumentType",
        "type",
        "marketStatus",
        "status",
        "dealStatus",
        "tradeStatus",
        "tradingStatus",
    ):
        if market.get(key):
            parts.append(str(market[key]))
    return " ".join(parts).lower()


def is_tradeable_market(market):
    text = market_text(market)
    blocked_terms = (
        "close only",
        "closing only",
        "close-only",
        "closing-only",
        "closings only",
        "view only",
        "view-only",
        "reduce only",
        "reduce-only",
        "disabled",
        "suspended",
        "delisted",
        "expired",
        "unavailable",
        "not tradeable",
        "not tradable",
        "not available",
        "cannot open",
        "open disabled",
        "opening disabled",
    )
    if any(term in text for term in blocked_terms):
        return False

    allowed_statuses = ("open", "closed", "tradeable", "tradable", "normal", "online", "active")
    for key in ("marketStatus", "status", "dealStatus", "tradeStatus", "tradingStatus"):
        value = str(market.get(key) or "").strip().lower()
        if value and value not in allowed_statuses:
            return False

    for key in ("canOpenPosition", "canOpenPositions", "openingAllowed", "isTradeable", "isTradable"):
        if market.get(key) is False:
            return False

    return True


def is_etf(market):
    if not is_tradeable_market(market):
        return False
    name = str(market.get("instrumentName") or market.get("name") or "").lower()
    epic = str(market.get("epic") or "").lower()
    padded_name = f" {name} "
    if " etf" in padded_name or "exchange traded" in name or " ucits" in padded_name or epic.endswith("etf"):
        return True
    company_terms = (" inc", " plc", " ltd", " limited", " corp", " corporation", " bancorp")
    if any(term in padded_name for term in company_terms):
        return False
    return " fund" in padded_name


def is_stock(market):
    if not is_tradeable_market(market):
        return False
    if is_etf(market):
        return False
    text = market_text(market)
    name = str(market.get("instrumentName") or market.get("name") or "").lower()
    padded_name = f" {name} "
    excluded_terms = (
        " index",
        "indices",
        "commodity",
        "forex",
        "currency",
        "crypto",
        "future",
        "fund",
        "bond",
        "treasury",
        "rate",
    )
    if any(term in text for term in excluded_terms):
        return False
    stock_terms = (" share", " shares", " stock", " equity", " equities")
    company_terms = (" inc", " plc", " ltd", " limited", " corp", " corporation", " nv", " sa", " ag", " adr")
    return any(term in text for term in stock_terms) or any(term in padded_name for term in company_terms)


def instrument_matches(market, kind):
    if kind == "etf":
        return is_etf(market)
    if kind == "stock":
        return is_stock(market)
    raise ValueError(f"Unsupported instrument kind: {kind}")


def discover_instruments(client, kind):
    markets = []

    try:
        payload = client.get("/markets")
        markets.extend(extract_markets(payload))
    except Exception as exc:
        print(f"GET /markets failed: {exc}")

    if kind == "stock" or not any(instrument_matches(market, kind) for market in markets):
        roots = client.get("/marketnavigation")
        nodes = roots.get("nodes") or roots.get("marketNavigation") or []
        queue = [(node, [str(node.get("name") or node.get("id") or "")]) for node in nodes]
        seen = set()
        while queue:
            node, path = queue.pop(0)
            node_id = str(node.get("id", ""))
            if not node_id or node_id in seen:
                continue
            seen.add(node_id)
            node_payload = client.get(f"/marketnavigation/{urllib.parse.quote(node_id)}?limit=500")
            markets.extend(extract_navigation_markets(node_payload, path))
            for child in node_payload.get("nodes", []) or node_payload.get("marketNavigation", []) or []:
                child_name = str(child.get("name") or child.get("id") or "")
                queue.append((child, [*path, child_name]))
            time.sleep(0.15)

    instruments = [market for market in markets if instrument_matches(market, kind)]
    deduped = {}
    for market in instruments:
        deduped[market["epic"]] = market
    if kind == "stock":
        for term in ALWAYS_INCLUDE_STOCK_TERMS:
            try:
                payload = client.get(f"/markets?searchTerm={urllib.parse.quote(term)}")
                for market in extract_markets(payload):
                    if instrument_matches(market, kind):
                        deduped[market["epic"]] = market
            except Exception as exc:
                print(f"GET /markets?searchTerm={term} failed: {exc}")
    return sorted(deduped.values(), key=lambda market: market.get("instrumentName") or market["epic"])


def is_always_include_stock(market):
    text = market_text(market)
    return any(term in text for term in ALWAYS_INCLUDE_STOCK_TERMS)


def first_market_value(market, keys):
    for key in keys:
        value = market.get(key)
        if value:
            return str(value)
    return ""


def first_upper_market_value(market, keys):
    return first_market_value(market, keys).upper()


def navigation_sector(market):
    for part in market.get("navigationPath") or []:
        normalized = str(part or "").strip().lower().replace("_", " ")
        if normalized in SECTOR_NAMES:
            return SECTOR_NAMES[normalized]
    return ""


def discover_etfs(client):
    return discover_instruments(client, "etf")


def price_value(price_obj):
    if isinstance(price_obj, dict):
        for key in ("bid", "ask", "lastTraded", "mid"):
            value = price_obj.get(key)
            if isinstance(value, (int, float)):
                return float(value)
    if isinstance(price_obj, (int, float)):
        return float(price_obj)
    return None


def parse_price_row(row):
    snapshot = row.get("snapshotTimeUTC") or row.get("snapshotTime") or row.get("time")
    if not snapshot:
        return None
    row_date = str(snapshot)[:10]
    close = price_value(row.get("closePrice")) or price_value(row.get("lastTradedPrice"))
    high = price_value(row.get("highPrice")) or close
    low = price_value(row.get("lowPrice")) or close
    open_price = price_value(row.get("openPrice")) or close
    if close is None:
        return None
    return {
        "date": row_date,
        "open": round(open_price, 6),
        "high": round(high, 6),
        "low": round(low, 6),
        "close": round(close, 6),
    }


def parse_intraday_row(row):
    snapshot = row.get("snapshotTimeUTC") or row.get("snapshotTime") or row.get("time")
    if not snapshot:
        return None
    close = price_value(row.get("closePrice")) or price_value(row.get("lastTradedPrice"))
    if close is None:
        return None
    timestamp = str(snapshot).replace(" ", "T")[:16]
    return {"date": timestamp, "close": round(close, 6)}


def fetch_prices(client, epic):
    path = f"/prices/{urllib.parse.quote(epic)}?resolution=DAY&max=1000"
    payload = client.get(path)
    prices = payload.get("prices") or []
    rows = [parse_price_row(row) for row in prices]
    rows = [row for row in rows if row]
    deduped = {row["date"]: row for row in rows}
    return [deduped[key] for key in sorted(deduped)]


def fetch_hourly_prices(client, epic, max_points=72):
    path = f"/prices/{urllib.parse.quote(epic)}?resolution=HOUR&max={max_points}"
    payload = client.get(path)
    prices = payload.get("prices") or []
    rows = [parse_intraday_row(row) for row in prices]
    rows = [row for row in rows if row]
    deduped = {row["date"]: row for row in rows}
    return [deduped[key] for key in sorted(deduped)]


def intraday_points(rows):
    if not rows:
        return []
    return [{"d": row["date"], "p": row["close"]} for row in rows if row.get("close")]


def fetch_market_details(client, market):
    try:
        payload = client.get(f"/markets/{urllib.parse.quote(market['epic'])}")
        details = extract_markets(payload)
        if details:
            enriched = dict(market)
            enriched.update(details[0])
            return enriched
    except Exception as exc:
        print(f"GET /markets/{market.get('epic')} failed: {exc}")
    return market


def enrich_market_details(client, markets):
    enriched = {market["epic"]: dict(market) for market in markets}
    epics = list(enriched)
    for start in range(0, len(epics), 50):
        batch = epics[start:start + 50]
        try:
            query = urllib.parse.quote(",".join(batch), safe=",")
            payload = client.get(f"/markets?epics={query}")
            for details in extract_markets(payload):
                epic = details.get("epic")
                if epic in enriched:
                    enriched[epic].update(details)
        except Exception as exc:
            print(f"GET /markets?epics=batch failed: {exc}")
        time.sleep(0.15)
    return [enriched[market["epic"]] for market in markets]


def closest_on_or_before(rows, target):
    best = None
    for row in rows:
        row_date = date.fromisoformat(row["date"])
        if row_date <= target:
            best = row
        else:
            break
    return best


def period_return(rows, end, days):
    old = closest_on_or_before(rows, date.fromisoformat(end["date"]) - timedelta(days=days))
    if not old or not old.get("close"):
        return None
    return (end["close"] / old["close"] - 1) * 100


def previous_year_low(rows, end):
    year = date.fromisoformat(end["date"]).year - 1
    candidates = [row for row in rows if date.fromisoformat(row["date"]).year == year]
    if not candidates:
        return None
    return min(candidates, key=lambda row: row["low"])


def moving_average(rows, period, offset=0):
    end = len(rows) - offset
    start = end - period
    if start < 0 or end <= 0:
        return None
    values = [row["close"] for row in rows[start:end] if row.get("close")]
    if len(values) != period:
        return None
    return sum(values) / period


def pct_change(current, previous):
    if current is None or previous in (None, 0):
        return None
    return (current / previous - 1) * 100


def clamp(value, low, high):
    return max(low, min(high, value))


def scale(value, low, high, points):
    if value is None:
        return 0
    return clamp((value - low) / (high - low), 0, 1) * points


def investment_signal(trend_score, dip_score, recovery_score, distance):
    if trend_score >= 45 and distance is not None and distance <= 5 and recovery_score >= 8:
        return "Uptrend dip"
    if trend_score >= 45 and distance is not None and distance <= 5:
        return "Uptrend near low"
    if trend_score >= 35 and recovery_score >= 10:
        return "Recovering trend"
    if distance is not None and distance < -20:
        return "Deep break"
    return "Watch"


def investment_metrics(rows, end, low, returns):
    ma50 = moving_average(rows, 50)
    ma200 = moving_average(rows, 200)
    prev_ma50 = moving_average(rows, 50, offset=30)
    prev_ma200 = moving_average(rows, 200, offset=60)
    ma50_slope = pct_change(ma50, prev_ma50)
    ma200_slope = pct_change(ma200, prev_ma200)
    distance = (end["close"] / low["low"] - 1) * 100 if low else None

    trend_score = 0
    if ma50 and ma200 and ma50 > ma200:
        trend_score += 25
    trend_score += scale(ma50_slope, -3, 8, 18)
    trend_score += scale(ma200_slope, -2, 5, 22)
    if ma200 and end["close"] > ma200:
        trend_score += 10
    trend_score = round(trend_score, 2)

    if distance is None:
        dip_score = 0
    elif distance < -25:
        dip_score = 8
    elif distance < -5:
        dip_score = 32
    elif distance <= 5:
        dip_score = 35
    elif distance <= 20:
        dip_score = 28 - ((distance - 5) / 15) * 8
    elif distance <= 50:
        dip_score = 18 - ((distance - 20) / 30) * 12
    else:
        dip_score = 0
    dip_score = round(max(0, dip_score), 2)

    recovery_score = 0
    if returns.get("return1w") is not None:
        recovery_score += scale(returns["return1w"], -2, 4, 8)
    if returns.get("return1m") is not None:
        recovery_score += scale(returns["return1m"], -5, 8, 9)
    if returns.get("return3m") is not None:
        recovery_score += scale(returns["return3m"], -10, 12, 8)
    recovery_score = round(recovery_score, 2)

    score = trend_score * 0.45 + dip_score * 0.35 + recovery_score * 0.20
    if trend_score < 25:
        score *= 0.55
    return {
        "ma50": round(ma50, 6) if ma50 is not None else None,
        "ma200": round(ma200, 6) if ma200 is not None else None,
        "ma50SlopePct": round(ma50_slope, 2) if ma50_slope is not None else None,
        "ma200SlopePct": round(ma200_slope, 2) if ma200_slope is not None else None,
        "trendScore": trend_score,
        "dipScore": dip_score,
        "recoveryScore": recovery_score,
        "investmentScore": round(score, 2),
        "investmentSignal": investment_signal(trend_score, dip_score, recovery_score, distance),
    }


def aggregate_points(rows, period):
    if period == "daily":
        selected = rows[-130:]
        label_len = 10
    else:
        selected_by_period = {}
        for row in rows:
            row_date = date.fromisoformat(row["date"])
            if period == "weekly":
                year, week, _ = row_date.isocalendar()
                key = f"{year}-W{week:02d}"
            else:
                key = row["date"][:7]
            selected_by_period[key] = row
        selected = [selected_by_period[key] for key in sorted(selected_by_period)]
        if period == "weekly":
            selected = selected[-156:]
        label_len = 7

    if not selected:
        return []
    first = selected[0]["close"]
    return [
        {"d": row["date"][:label_len], "v": round((row["close"] / first) * 100, 2), "p": row["close"]}
        for row in selected
        if first
    ]


def classify(items, metadata=None):
    ranked = sorted(
        [item for item in items if item.get("validated") and item.get("returnTotal") is not None],
        key=lambda item: item["returnTotal"],
        reverse=True,
    )
    quartile = math.ceil(len(ranked) * 0.25) if ranked else 0
    bottom_start = len(ranked) - quartile
    for index, item in enumerate(ranked, start=1):
        item["performanceRank"] = index
        if index <= quartile:
            item["band"] = "Best"
        elif index > bottom_start:
            item["band"] = "Worst"
        else:
            item["band"] = "Mediocre"

    investment = sorted(
        [item for item in items if item.get("investmentScore") is not None],
        key=lambda item: (-item["investmentScore"], item["name"]),
    )
    for index, item in enumerate(investment, start=1):
        item["investmentRank"] = index

    quality_dip = sorted(
        [item for item in items if item.get("qualityDipScore") is not None],
        key=lambda item: (-item["qualityDipScore"], item["name"]),
    )
    for index, item in enumerate(quality_dip, start=1):
        item["qualityDipPartialRank"] = index

    items.sort(key=lambda item: (0 if item.get("validated") else 1, item.get("performanceRank") or 999999))
    dates = sorted({item["priceDate"] for item in items if item.get("priceDate")})
    return {
        "summary": {
            "generatedOn": date.today().isoformat(),
            "refreshedAtUtc": datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
            "sourceAsOf": dates[-1] if dates else "n/a",
            "instrumentCount": len(items),
            "validatedChartCount": len([item for item in items if item.get("validated")]),
            "source": "Capital.com demo API",
            **(metadata or {}),
            "classification": {
                "mappedCount": len([item for item in items if item.get("country") and item.get("sector")]),
                "unmappedCount": len([item for item in items if not item.get("country") or not item.get("sector")]),
                "providerStats": getattr(classify, "provider_stats", {}),
            },
        },
        "items": items,
    }


def build_item(market, rows, hourly_rows=None, kind=None):
    name = market.get("instrumentName") or market.get("symbol") or market["epic"]
    item = {
        "epic": market["epic"],
        "name": name,
        "symbol": market.get("symbol", ""),
        "instrumentType": market.get("instrumentType") or market.get("type") or "",
        "sector": first_market_value(market, ("sector", "sectorName", "industrySector", "marketSector")) or navigation_sector(market),
        "industry": first_market_value(market, ("industry", "industryName", "subsector", "subSector", "sectorSubType")),
        "country": first_market_value(market, ("country", "countryName", "countryOfOrigin")),
        "currency": first_upper_market_value(market, ("currency", "currencyCode", "quoteCurrency", "priceCurrency")),
        "exchange": first_market_value(market, ("exchange", "exchangeName", "exchangeShortName")),
        "region": first_market_value(market, ("region",)),
        "navigationPath": market.get("navigationPath") or [],
        "classificationSource": market.get("classificationSource") or "Capital.com",
        "classificationConfidence": market.get("classificationConfidence") or 0,
        "status": market.get("marketStatus") or market.get("status") or "",
        "validated": len(rows) > 1,
        "band": "Unvalidated",
        "performanceRank": None,
        "investmentRank": None,
    }
    if kind == "stock":
        item.update(quality_dip_metrics(rows, market.get("bid"), market.get("offer")))
    if len(rows) <= 1:
        return item

    start = rows[0]
    end = rows[-1]
    old_1y = closest_on_or_before(rows, date.fromisoformat(end["date"]) - timedelta(days=365))
    low = previous_year_low(rows, end)
    returns = {
        "return1w": period_return(rows, end, 7),
        "return1m": period_return(rows, end, 30),
        "return3m": period_return(rows, end, 91),
        "return6m": period_return(rows, end, 182),
        "return1y": period_return(rows, end, 365),
    }
    investing = investment_metrics(rows, end, low, returns)
    item.update(
        {
            "price": round(end["close"], 6),
            "priceDate": end["date"],
            "returnTotal": round((end["close"] / start["close"] - 1) * 100, 2) if start["close"] else None,
            "return1w": round(returns["return1w"], 2) if returns["return1w"] is not None else None,
            "return1m": round(returns["return1m"], 2) if returns["return1m"] is not None else None,
            "return3m": round(returns["return3m"], 2) if returns["return3m"] is not None else None,
            "return6m": round(returns["return6m"], 2) if returns["return6m"] is not None else None,
            "return1y": round(returns["return1y"], 2) if returns["return1y"] is not None else None,
            "oneYearAgoPrice": round(old_1y["close"], 6) if old_1y else None,
            "oneYearAgoDate": old_1y["date"] if old_1y else None,
            "previousYearLowPrice": round(low["low"], 6) if low else None,
            "previousYearLowDate": low["date"] if low else None,
            "distanceFromPreviousYearLowPct": round((end["close"] / low["low"] - 1) * 100, 2) if low else None,
            "days": (date.fromisoformat(end["date"]) - date.fromisoformat(start["date"])).days,
            "monthlyPoints": aggregate_points(rows, "monthly"),
            "weeklyPoints": aggregate_points(rows, "weekly"),
            "dailyPoints": aggregate_points(rows, "daily"),
            "hourlyPoints": intraday_points(hourly_rows or []),
        }
    )
    item.update(investing)
    return item


def run(output_path, kind="etf", label="ETF", limit=None, offset=0, metadata=None):
    client = CapitalClient()
    client.login()
    instruments = discover_instruments(client, kind)
    if limit is not None:
        selected = instruments[offset:offset + limit]
        if kind == "stock":
            selected_epics = {market["epic"] for market in selected}
            extras = [market for market in instruments if is_always_include_stock(market) and market["epic"] not in selected_epics]
            selected.extend(extras)
        instruments = selected
    if not instruments:
        raise RuntimeError(f"No {label} instruments found in Capital.com market discovery.")

    if kind == "stock":
        instruments = enrich_market_details(client, instruments)
        instruments, provider_stats = enrich_classification(instruments)
        classify.provider_stats = provider_stats
        for market in instruments:
            if not market.get("region"):
                market["region"] = region_for(market.get("country"), market.get("currency"))
    else:
        classify.provider_stats = {}

    items = []
    for index, market in enumerate(instruments, start=1):
        print(f"[{index}/{len(instruments)}] {market.get('epic')} {market.get('instrumentName')}", flush=True)
        try:
            rows = fetch_prices(client, market["epic"])
            try:
                hourly_rows = fetch_hourly_prices(client, market["epic"])
            except Exception as intraday_exc:
                print(f"Hourly prices unavailable for {market.get('epic')}: {intraday_exc}", flush=True)
                hourly_rows = []
            items.append(build_item(market, rows, hourly_rows, kind=kind))
        except Exception as exc:
            if kind == "stock":
                item = build_item(market, [], kind="stock")
                item.update({"validated": False, "band": "Unvalidated", "error": str(exc)})
                items.append(item)
            else:
                items.append(
                    {
                        "epic": market["epic"],
                        "name": market.get("instrumentName") or market["epic"],
                        "instrumentType": market.get("instrumentType") or "",
                        "validated": False,
                        "band": "Unvalidated",
                        "error": str(exc),
                    }
                )
        time.sleep(0.15)

    output = Path(output_path)
    output.parent.mkdir(parents=True, exist_ok=True)
    output_metadata = dict(metadata or {})
    if kind == "stock":
        output_metadata["qualityDipScoringVersion"] = QUALITY_DIP_SCORING_VERSION
    output.write_text(json.dumps(classify(items, output_metadata), indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Wrote {output}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="data/etfs.raw.json")
    parser.add_argument("--limit", type=int, default=None)
    args = parser.parse_args()
    run(args.output, kind="etf", label="ETF", limit=args.limit)


if __name__ == "__main__":
    main()
