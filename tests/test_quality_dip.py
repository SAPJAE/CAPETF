import math
import unittest
from datetime import date, timedelta

from scripts.quality_dip import quality_dip_metrics


def recent_friday():
    return date.today() - timedelta(days=(date.today().weekday() - 4) % 7)


def weekly_rows(closes, end=None):
    end = end or recent_friday()
    start = end - timedelta(weeks=len(closes) - 1)
    return [
        {
            "date": (start + timedelta(weeks=index)).isoformat(),
            "close": round(close, 6),
            "high": round(close * 1.01, 6),
            "low": round(close * 0.99, 6),
        }
        for index, close in enumerate(closes)
    ]


def smooth_uptrend(weeks=190):
    return [100 * math.exp(0.0025 * week) for week in range(weeks)]


def stabilizing_dip():
    closes = smooth_uptrend(160)
    peak = closes[-1]
    closes.extend(peak * (0.985 ** step) for step in range(1, 26))
    low = closes[-1]
    closes.extend(low * factor for factor in (1.015, 1.025, 1.035, 1.04, 1.045))
    return closes


def persistent_collapse(weeks=190):
    return [160 * math.exp(-0.012 * week) for week in range(weeks)]


def older_peak_then_flat(weeks=190):
    closes = [100 + 5 * week for week in range(21)]
    closes.extend(200 - 5 * week for week in range(1, 21))
    closes.extend([100] * (weeks - len(closes)))
    return closes


def sideways_history(weeks=190):
    return [100] * weeks


def volatile_spike(rows, index=-1, high=200, low=99):
    observed = date.fromisoformat(rows[index]["date"])
    return {
        "date": (observed - timedelta(days=2)).isoformat(),
        "close": rows[index]["close"],
        "high": high,
        "low": low,
    }


class QualityDipMetricsTests(unittest.TestCase):
    def test_returns_unrated_for_fewer_than_120_weekly_observations(self):
        result = quality_dip_metrics(weekly_rows(smooth_uptrend(119)))

        self.assertEqual("Unrated", result["qualityDipLabel"])
        self.assertIsNone(result["qualityDipScore"])
        self.assertIsNone(result["qualityDipTrendScore"])
        self.assertEqual("quality-dip-v1", result["qualityDipVersion"])

    def test_returns_unrated_when_120_weeks_do_not_span_three_years(self):
        result = quality_dip_metrics(weekly_rows(smooth_uptrend(120)))

        self.assertEqual("Unrated", result["qualityDipLabel"])
        self.assertIsNone(result["qualityDipScore"])

    def test_returns_unrated_when_latest_week_is_more_than_ten_days_old(self):
        stale_end = date.today() - timedelta(days=11)

        result = quality_dip_metrics(weekly_rows(smooth_uptrend(), end=stale_end))

        self.assertEqual("Unrated", result["qualityDipLabel"])
        self.assertIsNone(result["qualityDipScore"])

    def test_stabilizing_dip_outranks_a_near_high_uptrend_and_collapse(self):
        near_high = quality_dip_metrics(weekly_rows(smooth_uptrend()))
        dip = quality_dip_metrics(weekly_rows(stabilizing_dip()))
        collapse = quality_dip_metrics(weekly_rows(persistent_collapse()))

        self.assertGreater(dip["qualityDipScore"], near_high["qualityDipScore"])
        self.assertGreater(dip["qualityDipScore"], collapse["qualityDipScore"])
        self.assertGreater(dip["qualityDipDiscountScore"], near_high["qualityDipDiscountScore"])

    def test_extreme_collapse_over_70_percent_is_capped_and_weak(self):
        closes = smooth_uptrend(145)
        peak = closes[-1]
        closes.extend(peak * (0.95 ** step) for step in range(1, 46))

        result = quality_dip_metrics(weekly_rows(closes))

        self.assertGreater(result["qualityDipDrawdownPct"], 70)
        self.assertLessEqual(result["qualityDipScore"], 55)
        self.assertEqual("Broken or weak trend", result["qualityDipLabel"])

    def test_is_deterministic_and_bounds_every_component(self):
        rows = weekly_rows(stabilizing_dip())
        first = quality_dip_metrics(rows, bid=99.9, offer=100.1)
        second = quality_dip_metrics(list(reversed(rows)), bid=99.9, offer=100.1)

        self.assertEqual(first, second)
        self.assertIsNone(first["qualityDipRank"])
        for field in (
            "qualityDipScore",
            "qualityDipTrendScore",
            "qualityDipDiscountScore",
            "qualityDipStabilizationScore",
            "qualityDipRiskScore",
            "qualityDipDrawdownPct",
            "qualityDipTrendDistancePct",
        ):
            self.assertGreaterEqual(first[field], 0, field)
            self.assertLessEqual(first[field], 100, field)
            self.assertEqual(first[field], round(first[field], 2), field)

    def test_missing_spread_normalizes_available_risk_to_ten_points(self):
        rows = weekly_rows(smooth_uptrend())
        for row in rows:
            row["high"] = row["close"]
        result = quality_dip_metrics(rows, bid=None, offer=None)

        self.assertEqual(10.0, result["qualityDipRiskScore"])

    def test_risk_uses_drawdown_from_the_all_time_peak_after_52_flat_weeks(self):
        result = quality_dip_metrics(weekly_rows(older_peak_then_flat()), bid=100, offer=100.1)

        self.assertEqual(0.99, result["qualityDipDrawdownPct"])
        self.assertEqual(6.0, result["qualityDipRiskScore"])

    def test_no_new_13_week_low_awards_a_one_percent_rebound(self):
        result = quality_dip_metrics(weekly_rows([100] * 189 + [101]))

        self.assertEqual(20.0, result["qualityDipStabilizationScore"])

    def test_no_new_13_week_low_awards_an_equal_close(self):
        result = quality_dip_metrics(weekly_rows([100] * 190))

        self.assertEqual(15.0, result["qualityDipStabilizationScore"])

    def test_missing_spread_normalizes_a_partial_risk_subtotal(self):
        result = quality_dip_metrics(weekly_rows(older_peak_then_flat()), bid=None, offer=None)

        self.assertEqual(4.29, result["qualityDipRiskScore"])

    def test_invalid_quotes_are_treated_as_missing_and_normalize_risk(self):
        rows = weekly_rows(older_peak_then_flat())
        expected = quality_dip_metrics(rows, bid=None, offer=None)["qualityDipRiskScore"]

        for bid, offer in (
            (0, 100),
            (100, 0),
            (-1, 100),
            (100, -1),
            (math.nan, 100),
            (100, math.inf),
            (101, 100),
        ):
            with self.subTest(bid=bid, offer=offer):
                result = quality_dip_metrics(rows, bid=bid, offer=offer)
                self.assertEqual(expected, result["qualityDipRiskScore"])

    def test_daily_rows_use_the_final_valid_observation_of_each_iso_week(self):
        baseline = weekly_rows(smooth_uptrend())
        final_date = date.fromisoformat(baseline[-1]["date"])
        earlier_same_week = {
            "date": (final_date - timedelta(days=2)).isoformat(),
            "close": baseline[-1]["close"] * 0.5,
            "high": baseline[-1]["high"],
            "low": baseline[-1]["low"],
        }

        result = quality_dip_metrics([earlier_same_week, *reversed(baseline)])

        self.assertEqual(quality_dip_metrics(baseline), result)

    def test_intraweek_high_spike_changes_trailing_and_historical_drawdowns(self):
        baseline = weekly_rows(sideways_history())
        spiked = quality_dip_metrics([volatile_spike(baseline), *baseline], bid=100, offer=100.1)
        ordinary = quality_dip_metrics(baseline, bid=100, offer=100.1)
        trending = weekly_rows(smooth_uptrend())
        historical_spike = volatile_spike(trending, index=20, high=140, low=trending[20]["low"])
        recovered = quality_dip_metrics([historical_spike, *trending], bid=100, offer=100.1)
        ordinary_trend = quality_dip_metrics(trending, bid=100, offer=100.1)

        self.assertGreater(spiked["qualityDipDrawdownPct"], 45)
        self.assertGreater(spiked["qualityDipDrawdownPct"], ordinary["qualityDipDrawdownPct"])
        self.assertGreater(recovered["qualityDipRiskScore"], ordinary_trend["qualityDipRiskScore"])

    def test_intraweek_previous_year_low_changes_low_proximity_discount(self):
        baseline = weekly_rows(sideways_history())
        latest_year = date.fromisoformat(baseline[-1]["date"]).year
        target = next(index for index, row in enumerate(baseline) if date.fromisoformat(row["date"]).year == latest_year - 1)
        low_spike = volatile_spike(baseline, index=target, high=100, low=50)

        ordinary = quality_dip_metrics(baseline)
        spiked = quality_dip_metrics([low_spike, *baseline])

        self.assertLess(spiked["qualityDipDiscountScore"], ordinary["qualityDipDiscountScore"])


if __name__ == "__main__":
    unittest.main()
