import argparse
import json
import math
import os
import time
import urllib.parse
import urllib.request
from datetime import date, datetime, timezone, timedelta
from pathlib import Path


DEMO_BASE_URL = "https://demo-api-capital.backend-capital.com/api/v1"
LIVE_BASE_URL = "https://api-capital.backend-capital.com/api/v1"


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
        with urllib.request.urlopen(req, timeout=45) as response:
            text = response.read().decode("utf-8")
            if path == "/session":
                self.cst = response.headers.get("CST", "")
                self.security_token = response.headers.get("X-SECURITY-TOKEN", "")
            return json.loads(text) if text else {}

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


def market_text(market):
    parts = []
    for key in ("instrumentName", "epic", "symbol", "instrumentType", "type", "marketStatus"):
        if market.get(key):
            parts.append(str(market[key]))
    return " ".join(parts).lower()


def is_etf(market):
    text = market_text(market)
    return "etf" in text or "exchange traded" in text


def discover_etfs(client):
    markets = []

    try:
        payload = client.get("/markets")
        markets.extend(extract_markets(payload))
    except Exception as exc:
        print(f"GET /markets failed: {exc}")

    if not any(is_etf(market) for market in markets):
        roots = client.get("/marketnavigation")
        nodes = roots.get("nodes") or roots.get("marketNavigation") or []
        queue = list(nodes)
        seen = set()
        while queue:
            node = queue.pop(0)
            node_id = str(node.get("id", ""))
            if not node_id or node_id in seen:
                continue
            seen.add(node_id)
            node_payload = client.get(f"/marketnavigation/{urllib.parse.quote(node_id)}?limit=500")
            markets.extend(extract_markets(node_payload))
            for child in node_payload.get("nodes", []) or node_payload.get("marketNavigation", []) or []:
                queue.append(child)
            time.sleep(0.15)

    etfs = [market for market in markets if is_etf(market)]
    deduped = {}
    for market in etfs:
        deduped[market["epic"]] = market
    return sorted(deduped.values(), key=lambda market: market.get("instrumentName") or market["epic"])


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


def fetch_prices(client, epic):
    path = f"/prices/{urllib.parse.quote(epic)}?resolution=DAY&max=1000"
    payload = client.get(path)
    prices = payload.get("prices") or []
    rows = [parse_price_row(row) for row in prices]
    rows = [row for row in rows if row]
    deduped = {row["date"]: row for row in rows}
    return [deduped[key] for key in sorted(deduped)]


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


def aggregate_points(rows, period):
    if period == "daily":
        selected = rows
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
        label_len = 7

    if not selected:
        return []
    first = selected[0]["close"]
    return [
        {"d": row["date"][:label_len], "v": round((row["close"] / first) * 100, 2), "p": row["close"]}
        for row in selected
        if first
    ]


def classify(items):
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
        [item for item in items if item.get("distanceFromPreviousYearLowPct") is not None],
        key=lambda item: (item["distanceFromPreviousYearLowPct"], item["name"]),
    )
    for index, item in enumerate(investment, start=1):
        item["investmentRank"] = index

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
        },
        "items": items,
    }


def build_item(market, rows):
    name = market.get("instrumentName") or market.get("symbol") or market["epic"]
    item = {
        "epic": market["epic"],
        "name": name,
        "symbol": market.get("symbol", ""),
        "instrumentType": market.get("instrumentType") or market.get("type") or "",
        "status": market.get("marketStatus") or market.get("status") or "",
        "validated": len(rows) > 1,
        "band": "Unvalidated",
        "performanceRank": None,
        "investmentRank": None,
    }
    if len(rows) <= 1:
        return item

    start = rows[0]
    end = rows[-1]
    old_1y = closest_on_or_before(rows, date.fromisoformat(end["date"]) - timedelta(days=365))
    low = previous_year_low(rows, end)
    item.update(
        {
            "price": round(end["close"], 6),
            "priceDate": end["date"],
            "returnTotal": round((end["close"] / start["close"] - 1) * 100, 2) if start["close"] else None,
            "return1w": round(period_return(rows, end, 7), 2) if period_return(rows, end, 7) is not None else None,
            "return1m": round(period_return(rows, end, 30), 2) if period_return(rows, end, 30) is not None else None,
            "return3m": round(period_return(rows, end, 91), 2) if period_return(rows, end, 91) is not None else None,
            "return6m": round(period_return(rows, end, 182), 2) if period_return(rows, end, 182) is not None else None,
            "return1y": round(period_return(rows, end, 365), 2) if period_return(rows, end, 365) is not None else None,
            "oneYearAgoPrice": round(old_1y["close"], 6) if old_1y else None,
            "oneYearAgoDate": old_1y["date"] if old_1y else None,
            "previousYearLowPrice": round(low["low"], 6) if low else None,
            "previousYearLowDate": low["date"] if low else None,
            "distanceFromPreviousYearLowPct": round((end["close"] / low["low"] - 1) * 100, 2) if low else None,
            "days": (date.fromisoformat(end["date"]) - date.fromisoformat(start["date"])).days,
            "monthlyPoints": aggregate_points(rows, "monthly"),
            "weeklyPoints": aggregate_points(rows, "weekly"),
            "dailyPoints": aggregate_points(rows, "daily"),
        }
    )
    return item


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="data/etfs.raw.json")
    args = parser.parse_args()

    client = CapitalClient()
    client.login()
    etfs = discover_etfs(client)
    if not etfs:
        raise RuntimeError("No ETF instruments found in Capital.com market discovery.")

    items = []
    for index, market in enumerate(etfs, start=1):
        print(f"[{index}/{len(etfs)}] {market.get('epic')} {market.get('instrumentName')}", flush=True)
        try:
            rows = fetch_prices(client, market["epic"])
            items.append(build_item(market, rows))
        except Exception as exc:
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

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(classify(items), indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Wrote {output}")


if __name__ == "__main__":
    main()
