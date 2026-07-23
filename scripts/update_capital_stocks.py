import argparse
import json
import math
import os
import time
from pathlib import Path

from update_capital_etfs import (
    CapitalClient,
    build_item,
    classify,
    discover_instruments,
    enrich_market_details,
    fetch_hourly_prices,
    fetch_prices,
    is_always_include_stock,
    QUALITY_DIP_SCORING_VERSION,
    run,
)
from stock_classification import enrich_classification, region_for


def write_stock_payload(output_path, items, metadata):
    output = Path(output_path)
    output.parent.mkdir(parents=True, exist_ok=True)
    output_metadata = {
        **metadata,
        "qualityDipScoringVersion": QUALITY_DIP_SCORING_VERSION,
        "refreshGeneration": os.environ.get("REFRESH_GENERATION", ""),
    }
    output.write_text(json.dumps(classify(items, output_metadata), indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Wrote {output}", flush=True)


def run_chunked(output_path, limit, offset, chunks, manifest_path):
    client = CapitalClient()
    client.login()
    instruments = discover_instruments(client, "stock")
    total_limit = limit * chunks
    selected = instruments[offset:offset + total_limit]
    selected_epics = {market["epic"] for market in selected}
    extras = [market for market in instruments if is_always_include_stock(market) and market["epic"] not in selected_epics]
    selected.extend(extras)
    if not selected:
        raise RuntimeError("No stock instruments found in Capital.com market discovery.")

    selected = enrich_market_details(client, selected)
    selected, provider_stats = enrich_classification(selected)
    classify.provider_stats = provider_stats
    for market in selected:
        if not market.get("region"):
            market["region"] = region_for(market.get("country"), market.get("currency"))

    output = Path(output_path)
    output.parent.mkdir(parents=True, exist_ok=True)
    base_name = output.name[:-len(".raw.json")] if output.name.endswith(".raw.json") else output.stem
    chunk_files = []
    total_chunks = math.ceil(len(selected) / limit)
    for chunk_index in range(total_chunks):
        chunk_markets = selected[chunk_index * limit:(chunk_index + 1) * limit]
        chunk_items = []
        for index, market in enumerate(chunk_markets, start=1):
            absolute_index = chunk_index * limit + index
            print(f"[{absolute_index}/{len(selected)}] {market.get('epic')} {market.get('instrumentName')}", flush=True)
            try:
                rows = fetch_prices(client, market["epic"])
                try:
                    hourly_rows = fetch_hourly_prices(client, market["epic"])
                except Exception as intraday_exc:
                    print(f"Hourly prices unavailable for {market.get('epic')}: {intraday_exc}", flush=True)
                    hourly_rows = []
                chunk_items.append(build_item(market, rows, hourly_rows, kind="stock"))
            except Exception as exc:
                item = build_item(market, [], kind="stock")
                item.update({"validated": False, "band": "Unvalidated", "error": str(exc)})
                chunk_items.append(item)
            time.sleep(0.15)

        chunk_output = output.parent / f"{base_name}-{chunk_index:03d}.raw.json"
        write_stock_payload(
            chunk_output,
            chunk_items,
            {
                "chunkIndex": chunk_index,
                "chunkCount": total_chunks,
                "offset": offset + (chunk_index * limit),
                "limit": limit,
                "totalRequested": total_limit,
            },
        )
        chunk_files.append(str(chunk_output).replace("\\", "/").replace(".raw.json", ".enc.json"))

    if manifest_path:
        Path(manifest_path).write_text(
            json.dumps(
                {
                    "chunkCount": total_chunks,
                    "chunkSize": limit,
                    "instrumentCount": len(selected),
                    "chunkFiles": chunk_files,
                },
                indent=2,
            ),
            encoding="utf-8",
        )


def run_batch(output_path, limit, offset, batch_index, batch_count):
    client = CapitalClient()
    client.login()
    instruments = discover_instruments(client, "stock")
    selected = instruments[offset:offset + limit]
    if batch_index == batch_count - 1:
        selected_epics = {market["epic"] for market in selected}
        extras = [market for market in instruments if is_always_include_stock(market) and market["epic"] not in selected_epics]
        selected.extend(extras)
    if not selected:
        raise RuntimeError("No stock instruments found in Capital.com market discovery.")

    selected = enrich_market_details(client, selected)
    selected, provider_stats = enrich_classification(selected)
    classify.provider_stats = provider_stats
    for market in selected:
        if not market.get("region"):
            market["region"] = region_for(market.get("country"), market.get("currency"))

    items = []
    for index, market in enumerate(selected, start=1):
        absolute_index = offset + index
        print(f"[batch {batch_index + 1}/{batch_count}] [{absolute_index}] {market.get('epic')} {market.get('instrumentName')}", flush=True)
        try:
            rows = fetch_prices(client, market["epic"])
            try:
                hourly_rows = fetch_hourly_prices(client, market["epic"])
            except Exception as intraday_exc:
                print(f"Hourly prices unavailable for {market.get('epic')}: {intraday_exc}", flush=True)
                hourly_rows = []
            items.append(build_item(market, rows, hourly_rows, kind="stock"))
        except Exception as exc:
            item = build_item(market, [], kind="stock")
            item.update({"validated": False, "band": "Unvalidated", "error": str(exc)})
            items.append(item)
        time.sleep(0.15)

    write_stock_payload(
        output_path,
        items,
        {
            "chunkIndex": batch_index,
            "chunkCount": batch_count,
            "offset": offset,
            "limit": limit,
            "totalRequested": limit * batch_count,
        },
    )


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="data/stocks.raw.json")
    parser.add_argument("--limit", type=int, default=250)
    parser.add_argument("--offset", type=int, default=0)
    parser.add_argument("--chunks", type=int, default=0)
    parser.add_argument("--manifest", default="")
    parser.add_argument("--batch-index", type=int, default=None)
    parser.add_argument("--batch-count", type=int, default=11)
    args = parser.parse_args()
    if args.batch_index is not None:
        run_batch(args.output, args.limit, args.offset, args.batch_index, args.batch_count)
        return
    if args.chunks:
        run_chunked(args.output, args.limit, args.offset, args.chunks, args.manifest)
        return
    run(
        args.output,
        kind="stock",
        label="stock",
        limit=args.limit,
        offset=args.offset,
        metadata={"refreshGeneration": os.environ.get("REFRESH_GENERATION", "")},
    )


if __name__ == "__main__":
    main()
