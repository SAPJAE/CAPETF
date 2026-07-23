import json
import os
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from scripts import encrypt_data


ROOT = Path(__file__).resolve().parents[1]


class EncryptionMetadataTests(unittest.TestCase):
    def test_metadata_summary_uses_only_the_public_stock_batch_fields(self):
        payload = {
            "summary": {
                "refreshGeneration": "123-2",
                "qualityDipScoringVersion": "quality-dip-v1",
                "chunkIndex": 4,
                "chunkCount": 11,
                "sourceAsOf": "2026-07-23",
                "refreshedAtUtc": "2026-07-23T12:00:00Z",
                "classification": {"provider": "secret detail"},
                "instrumentCount": 250,
            },
            "items": [{"epic": "SECRET", "name": "Not public"}],
        }

        self.assertEqual(
            {
                "refreshGeneration": "123-2",
                "qualityDipScoringVersion": "quality-dip-v1",
                "chunkIndex": 4,
                "chunkCount": 11,
                "sourceAsOf": "2026-07-23",
                "refreshedAtUtc": "2026-07-23T12:00:00Z",
            },
            encrypt_data.public_metadata(payload),
        )

    def test_metadata_output_writes_the_structured_summary_beside_encrypted_data(self):
        payload = {
            "summary": {
                "refreshGeneration": "123-2",
                "qualityDipScoringVersion": "quality-dip-v1",
                "chunkIndex": 0,
                "chunkCount": 11,
                "sourceAsOf": "2026-07-23",
                "refreshedAtUtc": "2026-07-23T12:00:00Z",
            },
            "items": [{"epic": "SECRET"}],
        }
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "stocks.raw.json"
            encrypted = Path(directory) / "stocks.enc.json"
            metadata = Path(directory) / "stocks.meta.json"
            source.write_text(json.dumps(payload), encoding="utf-8")
            argv = [
                "encrypt_data.py",
                "--input",
                str(source),
                "--output",
                str(encrypted),
                "--metadata-output",
                str(metadata),
            ]

            with patch.object(sys, "argv", argv), patch.dict(os.environ, {"DASHBOARD_PASSWORD": "test-password"}):
                encrypt_data.main()

            self.assertTrue(encrypted.exists())
            self.assertEqual(encrypt_data.public_metadata(payload), json.loads(metadata.read_text(encoding="utf-8")))

    def test_stock_workflow_uses_attempt_generation_and_commits_metadata_sidecars(self):
        workflow = (ROOT / ".github" / "workflows" / "refresh.yml").read_text(encoding="utf-8")

        self.assertIn("REFRESH_GENERATION: ${{ github.run_id }}-${{ github.run_attempt }}", workflow)
        self.assertIn("--metadata-output data/stocks-${{ matrix.batch }}.meta.json", workflow)
        self.assertIn("META_FILE: data/stocks-${{ matrix.batch }}.meta.json", workflow)
        self.assertIn('git add "$BATCH_FILE" "$META_FILE"', workflow)
        self.assertIn('"data/*.meta.json"', workflow)


if __name__ == "__main__":
    unittest.main()
