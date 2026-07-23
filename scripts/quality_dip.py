import math
import statistics
from datetime import date, datetime


VERSION = "quality-dip-v1"


def quality_dip_metrics(rows: list[dict], bid: float | None = None, offer: float | None = None) -> dict:
    weekly = _weekly_rows(rows)
    if not _is_eligible(weekly):
        return _unrated()

    closes = [row["close"] for row in weekly]
    regression = _log_regression(closes)
    trend_score = _trend_score(weekly, regression)
    drawdown_pct = _trailing_drawdown(closes)
    trend_distance_pct = _trend_distance(closes[-1], regression, len(closes) - 1)
    discount_score = _discount_score(weekly, drawdown_pct, trend_distance_pct)
    stabilization_score = _stabilization_score(closes)
    risk_score = _risk_score(closes, bid, offer)

    score = trend_score + discount_score + stabilization_score + risk_score
    if _sharply_falling_average(closes):
        score -= 15
    if _fresh_52_week_low(closes) and stabilization_score < 11:
        score -= 15
    if trend_score < 22:
        score = min(score, 49)
    if drawdown_pct > 70:
        score = min(score, 55)
    score = _bounded(score)
    if score >= 80 and not (trend_score >= 28 and stabilization_score >= 14):
        score = 79.99

    return {
        "qualityDipScore": score,
        "qualityDipLabel": _label(score, trend_score, stabilization_score),
        "qualityDipTrendScore": _bounded(trend_score),
        "qualityDipDiscountScore": _bounded(discount_score),
        "qualityDipStabilizationScore": _bounded(stabilization_score),
        "qualityDipRiskScore": _bounded(risk_score),
        "qualityDipDrawdownPct": _bounded(drawdown_pct),
        "qualityDipTrendDistancePct": _bounded(trend_distance_pct),
        "qualityDipRank": None,
        "qualityDipVersion": VERSION,
    }


def _weekly_rows(rows):
    latest_by_week = {}
    for row in rows:
        try:
            observed = _parse_date(row.get("date"))
            close = float(row.get("close"))
            high = float(row.get("high"))
            low = float(row.get("low"))
        except (AttributeError, TypeError, ValueError):
            continue
        if not all(math.isfinite(value) and value > 0 for value in (close, high, low)):
            continue
        year, week, _ = observed.isocalendar()
        key = (year, week)
        candidate = {"date": observed, "close": close}
        if key not in latest_by_week or observed >= latest_by_week[key]["date"]:
            latest_by_week[key] = candidate
    return [latest_by_week[key] for key in sorted(latest_by_week)]


def _parse_date(value):
    if isinstance(value, datetime):
        return value.date()
    if isinstance(value, date):
        return value
    return datetime.fromisoformat(str(value).replace("Z", "+00:00")).date()


def _is_eligible(weekly):
    return len(weekly) >= 120 and (weekly[-1]["date"] - weekly[0]["date"]).days >= 1095


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
        "qualityDipVersion": VERSION,
    }


def _log_regression(closes):
    count = len(closes)
    mean_x = (count - 1) / 2
    logs = [math.log(close) for close in closes]
    mean_y = statistics.fmean(logs)
    sum_xx = sum((index - mean_x) ** 2 for index in range(count))
    slope = sum((index - mean_x) * (value - mean_y) for index, value in enumerate(logs)) / sum_xx
    intercept = mean_y - slope * mean_x
    fitted = [intercept + slope * index for index in range(count)]
    total = sum((value - mean_y) ** 2 for value in logs)
    residual = sum((value - fit) ** 2 for value, fit in zip(logs, fitted))
    r_squared = 0.0 if total == 0 else max(0.0, 1 - residual / total)
    return {"slope": slope, "intercept": intercept, "r_squared": r_squared}


def _trend_score(weekly, regression):
    annualized_return = math.exp(regression["slope"] * 52) - 1
    annualized_points = _clamp(annualized_return / 0.20, 0, 1) * 12
    positive_year_points = _positive_year_ratio(weekly) * 10
    slope_points = 10 if regression["slope"] > 0 else 0
    return annualized_points + positive_year_points + slope_points + regression["r_squared"] * 8


def _positive_year_ratio(weekly):
    by_year = {}
    for row in weekly:
        by_year.setdefault(row["date"].year, []).append(row["close"])
    returns = [values[-1] > values[0] for values in by_year.values() if len(values) > 1]
    return sum(returns) / len(returns) if returns else 0.0


def _trailing_drawdown(closes):
    high = max(closes[-52:])
    return max(0.0, (high - closes[-1]) / high * 100)


def _trend_distance(close, regression, index):
    fitted_close = math.exp(regression["intercept"] + regression["slope"] * index)
    return max(0.0, (fitted_close - close) / fitted_close * 100)


def _discount_score(weekly, drawdown_pct, trend_distance_pct):
    drawdown_points = _pullback_points(drawdown_pct, 12)
    trend_points = _pullback_points(trend_distance_pct, 10)
    latest_year = weekly[-1]["date"].year
    previous_year_closes = [row["close"] for row in weekly if row["date"].year == latest_year - 1]
    if not previous_year_closes:
        previous_low_points = 0.0
    else:
        distance_above_low = max(0.0, (weekly[-1]["close"] / min(previous_year_closes) - 1) * 100)
        previous_low_points = _clamp(1 - distance_above_low / 25, 0, 1) * 8
    return drawdown_points + trend_points + previous_low_points


def _pullback_points(pullback_pct, maximum):
    if pullback_pct <= 5:
        return 0.0
    if pullback_pct <= 35:
        return (pullback_pct - 5) / 30 * maximum
    if pullback_pct <= 55:
        return maximum - (pullback_pct - 35) / 20 * (maximum / 2)
    return max(0.0, maximum / 2 - (pullback_pct - 55) / 15 * (maximum / 2))


def _stabilization_score(closes):
    prior_13_week_low = min(closes[-14:-1])
    no_new_low = 6 if closes[-1] > prior_13_week_low else 0
    current_four_week_return = closes[-1] / closes[-5] - 1
    prior_four_week_return = closes[-5] / closes[-9] - 1
    improving_return = 5 if current_four_week_return >= prior_four_week_return + 0.002 else 0
    current_average = statistics.fmean(closes[-10:])
    average_ratio = closes[-1] / current_average
    average_points = 5 if average_ratio >= 0.97 else 3 if average_ratio >= 0.94 else 0
    prior_average = statistics.fmean(closes[-14:-4])
    average_slope_points = 4 if current_average >= prior_average * 0.99 else 0
    return no_new_low + improving_return + average_points + average_slope_points


def _risk_score(closes, bid, offer):
    historical_drawdown = _worst_drawdown(closes)
    current_drawdown = _all_time_drawdown(closes)
    drawdown_points = 4 if historical_drawdown == 0 or current_drawdown < historical_drawdown * 0.8 else 0
    log_returns = [math.log(current / previous) for previous, current in zip(closes, closes[1:])]
    recent_volatility = _stdev(log_returns[-13:])
    historical_volatility = _stdev(log_returns[:-13])
    volatility_points = 3 if recent_volatility <= max(historical_volatility * 1.5, 1e-12) else 0
    if bid is None or offer is None:
        return (drawdown_points + volatility_points) * 10 / 7
    try:
        bid_value = float(bid)
        offer_value = float(offer)
    except (TypeError, ValueError):
        return (drawdown_points + volatility_points) * 10 / 7
    midpoint = (bid_value + offer_value) / 2
    spread_points = 3 if bid_value > 0 and offer_value >= bid_value and midpoint > 0 and (offer_value - bid_value) / midpoint * 100 <= 1 else 0
    return drawdown_points + volatility_points + spread_points


def _worst_drawdown(closes):
    peak = closes[0]
    worst = 0.0
    for close in closes:
        peak = max(peak, close)
        worst = max(worst, (peak - close) / peak * 100)
    return worst


def _all_time_drawdown(closes):
    peak = max(closes)
    return (peak - closes[-1]) / peak * 100


def _stdev(values):
    return statistics.pstdev(values) if len(values) > 1 else 0.0


def _sharply_falling_average(closes):
    return statistics.fmean(closes[-40:]) < statistics.fmean(closes[-44:-4]) * 0.95


def _fresh_52_week_low(closes):
    return min(closes[-2:]) < min(closes[-52:-2])


def _label(score, trend_score, stabilization_score):
    if score >= 80 and trend_score >= 28 and stabilization_score >= 14:
        return "Confirmed quality dip"
    if score >= 65:
        return "Stabilizing quality dip"
    if score >= 50:
        return "Watch"
    return "Broken or weak trend"


def _bounded(value):
    return round(_clamp(value, 0, 100), 2)


def _clamp(value, minimum, maximum):
    return max(minimum, min(maximum, value))
