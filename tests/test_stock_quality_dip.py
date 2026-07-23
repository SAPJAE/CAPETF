import math
import sys
import tempfile
import unittest
from datetime import date, timedelta
from pathlib import Path
from unittest.mock import patch


SCRIPTS_DIR = Path(__file__).resolve().parents[1] / "scripts"
if str(SCRIPTS_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_DIR))

import update_capital_etfs as pipeline
import update_capital_stocks as stocks


def generated_history(weeks=190):
    return [
        {
            "date": (date(2019, 1, 4) + timedelta(weeks=index)).isoformat(),
            "close": round(100 * math.exp(0.0025 * index), 6),
            "high": round(101 * math.exp(0.0025 * index), 6),
            "low": round(99 * math.exp(0.0025 * index), 6),
        }
        for index in range(weeks)
    ]


class FakeClient:
    def login(self):
        pass


class StockQualityDipTests(unittest.TestCase):
    def setUp(self):
        self.rows = generated_history()
        self.market = {
            "epic": "TEST",
            "instrumentName": "Test Company",
            "bid": 99.9,
            "offer": 100.1,
            "region": "United States",
        }

    def test_stock_build_item_includes_a_scored_quality_dip_version(self):
        item = pipeline.build_item(self.market, self.rows, kind="stock")

        self.assertIsNotNone(item["qualityDipScore"])
        self.assertEqual("quality-dip-v1", item["qualityDipVersion"])

    def test_stock_build_item_marks_short_history_unrated(self):
        item = pipeline.build_item(self.market, self.rows[:2], kind="stock")

        self.assertIsNone(item["qualityDipScore"])
        self.assertEqual("Unrated", item["qualityDipLabel"])

    def test_etf_and_unspecified_build_items_have_no_quality_dip_fields(self):
        etf = pipeline.build_item(self.market, self.rows, kind="etf")
        unspecified = pipeline.build_item(self.market, self.rows)

        self.assertFalse(any(key.startswith("qualityDip") for key in etf))
        self.assertFalse(any(key.startswith("qualityDip") for key in unspecified))

    def test_stock_build_item_forwards_bid_and_offer_to_the_scorer(self):
        metrics = {"qualityDipScore": 70, "qualityDipVersion": "quality-dip-v1"}
        with patch.object(pipeline, "quality_dip_metrics", return_value=metrics) as scorer:
            pipeline.build_item(self.market, self.rows[:2], kind="stock")

        scorer.assert_called_once_with(self.rows[:2], 99.9, 100.1)

    def test_classify_assigns_stock_partial_ranks_by_score_then_name(self):
        items = [
            {"name": "Zed", "validated": False, "qualityDipScore": 70},
            {"name": "Beta", "validated": False, "qualityDipScore": 80},
            {"name": "Alpha", "validated": False, "qualityDipScore": 80},
            {"name": "Unrated", "validated": False, "qualityDipScore": None},
        ]

        result = pipeline.classify(items)
        ranks = {item["name"]: item.get("qualityDipPartialRank") for item in result["items"]}

        self.assertEqual(1, ranks["Alpha"])
        self.assertEqual(2, ranks["Beta"])
        self.assertEqual(3, ranks["Zed"])
        self.assertIsNone(ranks["Unrated"])
        self.assertNotIn("qualityDipPartialRank", next(item for item in result["items"] if item["name"] == "Unrated"))

    def test_stock_batch_metadata_includes_the_scoring_version(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "stocks.raw.json"
            stocks.write_stock_payload(output, [], {})
            payload = pipeline.json.loads(output.read_text(encoding="utf-8"))

        self.assertEqual("quality-dip-v1", payload["summary"]["qualityDipScoringVersion"])

    def test_etf_metadata_excludes_the_scoring_version(self):
        result = pipeline.classify([], {"kind": "etf"})

        self.assertNotIn("qualityDipScoringVersion", result["summary"])

    def test_shared_run_passes_instrument_kind_to_build_item(self):
        expected_item = {"epic": "TEST", "name": "Test Company", "validated": False, "band": "Unvalidated"}
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "output.json"
            with patch.object(pipeline, "CapitalClient", return_value=FakeClient()), \
                 patch.object(pipeline, "discover_instruments", return_value=[self.market]), \
                 patch.object(pipeline, "enrich_market_details", side_effect=lambda client, markets: markets), \
                 patch.object(pipeline, "enrich_classification", side_effect=lambda markets: (markets, {})), \
                 patch.object(pipeline, "fetch_prices", return_value=self.rows), \
                 patch.object(pipeline, "fetch_hourly_prices", return_value=[]), \
                 patch.object(pipeline, "time") as mocked_time, \
                 patch.object(pipeline, "build_item", return_value=expected_item) as build:
                pipeline.run(output, kind="stock", label="stock")
                self.assertEqual("stock", build.call_args.kwargs["kind"])
                pipeline.run(output, kind="etf", label="ETF")
                self.assertEqual("etf", build.call_args.kwargs["kind"])
                payload = pipeline.json.loads(output.read_text(encoding="utf-8"))
                self.assertNotIn("qualityDipScoringVersion", payload["summary"])
                mocked_time.sleep.assert_called()

    def test_stock_batch_call_sites_pass_stock_kind_to_build_item(self):
        expected_item = {"epic": "TEST", "name": "Test Company", "validated": False, "band": "Unvalidated"}
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "stocks.raw.json"
            with patch.object(stocks, "CapitalClient", return_value=FakeClient()), \
                 patch.object(stocks, "discover_instruments", return_value=[self.market]), \
                 patch.object(stocks, "enrich_market_details", side_effect=lambda client, markets: markets), \
                 patch.object(stocks, "enrich_classification", side_effect=lambda markets: (markets, {})), \
                 patch.object(stocks, "fetch_prices", return_value=self.rows), \
                 patch.object(stocks, "fetch_hourly_prices", return_value=[]), \
                 patch.object(stocks, "write_stock_payload"), \
                 patch.object(stocks, "time") as mocked_time, \
                 patch.object(stocks, "build_item", return_value=expected_item) as build:
                stocks.run_batch(output, limit=1, offset=0, batch_index=0, batch_count=1)
                self.assertEqual("stock", build.call_args.kwargs["kind"])
                stocks.run_chunked(output, limit=1, offset=0, chunks=1, manifest_path="")
                self.assertEqual("stock", build.call_args.kwargs["kind"])
                mocked_time.sleep.assert_called()

    def test_shared_stock_run_marks_a_price_fetch_failure_unrated(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "stocks.raw.json"
            with patch.object(pipeline, "CapitalClient", return_value=FakeClient()), \
                 patch.object(pipeline, "discover_instruments", return_value=[self.market]), \
                 patch.object(pipeline, "enrich_market_details", side_effect=lambda client, markets: markets), \
                 patch.object(pipeline, "enrich_classification", side_effect=lambda markets: (markets, {})), \
                 patch.object(pipeline, "fetch_prices", side_effect=RuntimeError("price fetch failed")), \
                 patch.object(pipeline, "time"):
                pipeline.run(output, kind="stock", label="stock")
            item = pipeline.json.loads(output.read_text(encoding="utf-8"))["items"][0]

        self.assertIsNone(item["qualityDipScore"])
        self.assertEqual("Unrated", item["qualityDipLabel"])
        self.assertEqual("quality-dip-v1", item["qualityDipVersion"])
        self.assertFalse(item["validated"])
        self.assertEqual("Unvalidated", item["band"])
        self.assertEqual("price fetch failed", item["error"])

    def test_chunked_stock_run_marks_a_price_fetch_failure_unrated(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "stocks.raw.json"
            with patch.object(stocks, "CapitalClient", return_value=FakeClient()), \
                 patch.object(stocks, "discover_instruments", return_value=[self.market]), \
                 patch.object(stocks, "enrich_market_details", side_effect=lambda client, markets: markets), \
                 patch.object(stocks, "enrich_classification", side_effect=lambda markets: (markets, {})), \
                 patch.object(stocks, "fetch_prices", side_effect=RuntimeError("price fetch failed")), \
                 patch.object(stocks, "time"):
                stocks.run_chunked(output, limit=1, offset=0, chunks=1, manifest_path="")
            item = pipeline.json.loads((Path(directory) / "stocks-000.raw.json").read_text(encoding="utf-8"))["items"][0]

        self.assertIsNone(item["qualityDipScore"])
        self.assertEqual("Unrated", item["qualityDipLabel"])
        self.assertEqual("quality-dip-v1", item["qualityDipVersion"])
        self.assertFalse(item["validated"])
        self.assertEqual("Unvalidated", item["band"])
        self.assertEqual("price fetch failed", item["error"])

    def test_batched_stock_run_marks_a_price_fetch_failure_unrated(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "stocks.raw.json"
            with patch.object(stocks, "CapitalClient", return_value=FakeClient()), \
                 patch.object(stocks, "discover_instruments", return_value=[self.market]), \
                 patch.object(stocks, "enrich_market_details", side_effect=lambda client, markets: markets), \
                 patch.object(stocks, "enrich_classification", side_effect=lambda markets: (markets, {})), \
                 patch.object(stocks, "fetch_prices", side_effect=RuntimeError("price fetch failed")), \
                 patch.object(stocks, "time"):
                stocks.run_batch(output, limit=1, offset=0, batch_index=0, batch_count=1)
            item = pipeline.json.loads(output.read_text(encoding="utf-8"))["items"][0]

        self.assertIsNone(item["qualityDipScore"])
        self.assertEqual("Unrated", item["qualityDipLabel"])
        self.assertEqual("quality-dip-v1", item["qualityDipVersion"])
        self.assertFalse(item["validated"])
        self.assertEqual("Unvalidated", item["band"])
        self.assertEqual("price fetch failed", item["error"])


if __name__ == "__main__":
    unittest.main()
