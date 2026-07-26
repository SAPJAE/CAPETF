import unittest

from scripts.update_capital_etfs import aggregate_points


class AggregatePointsTests(unittest.TestCase):
    def test_weekly_retains_full_dates_while_monthly_uses_month_labels(self):
        rows = [
            {"date": "2026-01-02", "close": 100.0},
            {"date": "2026-01-09", "close": 101.0},
            {"date": "2026-02-06", "close": 102.0},
        ]

        self.assertEqual(
            ["2026-01-02", "2026-01-09", "2026-02-06"],
            [point["d"] for point in aggregate_points(rows, "weekly")],
        )
        self.assertEqual(
            ["2026-01", "2026-02"],
            [point["d"] for point in aggregate_points(rows, "monthly")],
        )


if __name__ == "__main__":
    unittest.main()
