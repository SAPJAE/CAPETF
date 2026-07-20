import json
import os
import re
import time
import urllib.parse
import urllib.request


EUROPE_COUNTRIES = {
    "austria", "belgium", "denmark", "finland", "france", "germany", "ireland",
    "italy", "netherlands", "norway", "poland", "portugal", "spain", "sweden",
    "switzerland", "united kingdom", "uk",
}
EUROPE_CURRENCIES = {"EUR", "GBP", "CHF", "SEK", "NOK", "DKK", "PLN"}
SECTOR_ALIASES = {
    "basic materials": "Materials",
    "communication services": "Communication Services",
    "communications": "Communication Services",
    "consumer cyclical": "Consumer Discretionary",
    "consumer defensive": "Consumer Staples",
    "consumer discretionary": "Consumer Discretionary",
    "consumer goods": "Consumer Staples",
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
    "life sciences": "Healthcare",
    "materials": "Materials",
    "real estate": "Real Estate",
    "technology": "Technology",
    "trade & services": "Consumer Discretionary",
    "utilities": "Utilities",
}


def clean(value):
    return str(value or "").strip()


def normalize_country(value):
    text = clean(value)
    upper = text.upper()
    if upper in {"US", "USA", "UNITED STATES OF AMERICA"}:
        return "United States"
    if upper in {"GB", "GBR", "UK"}:
        return "United Kingdom"
    return text


def normalize_sector(value):
    text = clean(value)
    if not text:
        return ""
    return SECTOR_ALIASES.get(text.lower(), text)


def region_for(country, currency):
    country_key = clean(country).lower()
    currency_key = clean(currency).upper()
    if country_key in {"united states", "usa", "us"}:
        return "US"
    if country_key == "canada":
        return "Canada"
    if country_key == "japan":
        return "Japan"
    if country_key in EUROPE_COUNTRIES or currency_key in EUROPE_CURRENCIES:
        return "Europe"
    if country_key in {"hong kong", "china", "singapore", "india", "australia"}:
        return "Asia Pacific"
    return "Other"


def candidate_symbols(market):
    raw_values = [
        market.get("symbol"),
        market.get("epic"),
        clean(market.get("symbol")).replace(".", "-"),
        clean(market.get("epic")).replace(".", "-"),
    ]
    symbols = []
    for raw in raw_values:
        value = clean(raw)
        if not value:
            continue
        symbols.append(value)
        symbols.append(value.upper())
        if value.endswith(("d", "l")) and len(value) > 2:
            symbols.append(value[:-1])
            symbols.append(value[:-1].upper())
    seen = set()
    result = []
    for symbol in symbols:
        key = symbol.upper()
        if key not in seen:
            seen.add(key)
            result.append(symbol)
    return result


def company_tokens(value):
    text = re.sub(r"[^a-z0-9 ]+", " ", clean(value).lower())
    drop = {
        "inc", "incorporated", "corp", "corporation", "plc", "ltd", "limited",
        "sa", "nv", "ag", "se", "adr", "the", "co", "company", "group",
    }
    return {part for part in text.split() if len(part) > 2 and part not in drop}


def identity_score(market, profile):
    capital_name = clean(market.get("instrumentName") or market.get("name"))
    profile_name = clean(profile.get("name") or profile.get("companyName") or profile.get("Name"))
    capital_symbol = clean(market.get("symbol") or market.get("epic")).upper()
    profile_symbol = clean(profile.get("symbol") or profile.get("ticker") or profile.get("Symbol")).upper()
    score = 0
    if capital_symbol and profile_symbol and capital_symbol.rstrip("DL") == profile_symbol.rstrip("DL"):
        score += 45
    capital_tokens = company_tokens(capital_name)
    profile_tokens = company_tokens(profile_name)
    if capital_tokens and profile_tokens:
        overlap = len(capital_tokens & profile_tokens) / max(1, len(capital_tokens | profile_tokens))
        score += int(overlap * 55)
    return score


def request_json(url, headers=None):
    req = urllib.request.Request(url, headers=headers or {})
    with urllib.request.urlopen(req, timeout=30) as response:
        text = response.read().decode("utf-8")
        return json.loads(text) if text else {}


class ProfileProvider:
    name = "provider"
    min_score = 55

    def __init__(self, key):
        self.key = key

    def fetch(self, market):
        raise NotImplementedError

    def profile(self, market):
        best = None
        for candidate in self.fetch(market):
            score = identity_score(market, candidate)
            if best is None or score > best[0]:
                best = (score, candidate)
        if not best or best[0] < self.min_score:
            return None
        mapped = self.map_profile(best[1])
        if not mapped.get("sector") or not mapped.get("country"):
            return None
        mapped["classificationSource"] = self.name
        mapped["classificationConfidence"] = best[0]
        return mapped

    def map_profile(self, profile):
        return {}


class FmpProvider(ProfileProvider):
    name = "FMP"

    def fetch(self, market):
        results = []
        for symbol in candidate_symbols(market)[:6]:
            url = f"https://financialmodelingprep.com/stable/profile?symbol={urllib.parse.quote(symbol)}&apikey={urllib.parse.quote(self.key)}"
            payload = request_json(url)
            if isinstance(payload, list):
                results.extend(payload)
            elif isinstance(payload, dict):
                results.append(payload)
            time.sleep(0.2)
        return results

    def map_profile(self, profile):
        return {
            "country": normalize_country(profile.get("country") or profile.get("countryName")),
            "currency": clean(profile.get("currency") or profile.get("reportedCurrency")).upper(),
            "exchange": clean(profile.get("exchangeShortName") or profile.get("exchange")),
            "sector": normalize_sector(profile.get("sector")),
            "industry": clean(profile.get("industry")),
        }


class AlphaVantageProvider(ProfileProvider):
    name = "Alpha Vantage"

    def fetch(self, market):
        results = []
        for symbol in candidate_symbols(market)[:4]:
            url = f"https://www.alphavantage.co/query?function=OVERVIEW&symbol={urllib.parse.quote(symbol)}&apikey={urllib.parse.quote(self.key)}"
            payload = request_json(url)
            if payload.get("Symbol"):
                results.append(payload)
            time.sleep(12.1)
        return results

    def map_profile(self, profile):
        return {
            "country": normalize_country(profile.get("Country")),
            "currency": clean(profile.get("Currency")).upper(),
            "exchange": clean(profile.get("Exchange")),
            "sector": normalize_sector(profile.get("Sector")),
            "industry": clean(profile.get("Industry")),
        }


class FinnhubProvider(ProfileProvider):
    name = "Finnhub"

    def fetch(self, market):
        results = []
        for symbol in candidate_symbols(market)[:6]:
            url = f"https://finnhub.io/api/v1/stock/profile2?symbol={urllib.parse.quote(symbol)}&token={urllib.parse.quote(self.key)}"
            payload = request_json(url)
            if payload.get("ticker") or payload.get("name"):
                results.append(payload)
            time.sleep(0.2)
        return results

    def map_profile(self, profile):
        return {
            "country": normalize_country(profile.get("country")),
            "currency": clean(profile.get("currency")).upper(),
            "exchange": clean(profile.get("exchange")),
            "sector": normalize_sector(profile.get("finnhubIndustry")),
            "industry": clean(profile.get("finnhubIndustry")),
        }


class EodhdProvider(ProfileProvider):
    name = "EODHD"

    def fetch(self, market):
        results = []
        for symbol in candidate_symbols(market)[:6]:
            url = f"https://eodhd.com/api/fundamentals/{urllib.parse.quote(symbol)}?api_token={urllib.parse.quote(self.key)}&fmt=json&filter=General"
            payload = request_json(url)
            if isinstance(payload, dict) and (payload.get("Name") or payload.get("Code")):
                results.append(payload)
            time.sleep(0.2)
        return results

    def map_profile(self, profile):
        return {
            "country": normalize_country(profile.get("CountryName") or profile.get("CountryISO")),
            "currency": clean(profile.get("CurrencyCode")).upper(),
            "exchange": clean(profile.get("Exchange")),
            "sector": normalize_sector(profile.get("Sector")),
            "industry": clean(profile.get("Industry")),
        }


def active_providers():
    providers = []
    if os.environ.get("FMP_API_KEY"):
        providers.append(FmpProvider(os.environ["FMP_API_KEY"]))
    if os.environ.get("FINNHUB_API_KEY"):
        providers.append(FinnhubProvider(os.environ["FINNHUB_API_KEY"]))
    if os.environ.get("EODHD_API_KEY"):
        providers.append(EodhdProvider(os.environ["EODHD_API_KEY"]))
    if os.environ.get("ALPHA_VANTAGE_API_KEY"):
        providers.append(AlphaVantageProvider(os.environ["ALPHA_VANTAGE_API_KEY"]))
    return providers


def enrich_classification(markets):
    providers = active_providers()
    enriched = []
    stats = {"providers": [provider.name for provider in providers], "mapped": 0, "unmapped": 0}
    for market in markets:
        updated = dict(market)
        profile = None
        for provider in providers:
            try:
                profile = provider.profile(updated)
            except Exception as exc:
                print(f"{provider.name} classification failed for {updated.get('epic')}: {exc}", flush=True)
                profile = None
            if profile:
                break
        if profile:
            updated.update(profile)
            updated["region"] = region_for(updated.get("country"), updated.get("currency"))
            stats["mapped"] += 1
        else:
            updated.setdefault("country", "")
            updated.setdefault("currency", "")
            updated.setdefault("exchange", "")
            updated.setdefault("region", "")
            updated.setdefault("classificationSource", "Capital.com")
            updated.setdefault("classificationConfidence", 0)
            stats["unmapped"] += 1
        enriched.append(updated)
    return enriched, stats
