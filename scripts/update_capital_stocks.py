import argparse
import json
import math
import time
from pathlib import Path

from update_capital_etfs import (
    CapitalClient,
    build_item,
    classify,
    discover_instruments,
    enrich_market_details,
    fetch_prices,
    is_always_include_stock,
    run,
)
from stock_classification import enrich_classification, region_for


def write_stock_payload(output_path, items, metadata):
    output = Path(output_path)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(classify(items, metadata), indent=2, ensure_ascii=False), encoding="utf-8")
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
                chunk_items.append(build_item(market, rows))
            except Exception as exc:
                chunk_items.append(
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

        chunk_output = output.parent / f"{output.stem}-{chunk_index:03d}{output.suffix}"
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


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="data/stocks.raw.json")
    parser.add_argument("--limit", type=int, default=250)
    parser.add_argument("--offset", type=int, default=0)
    parser.add_argument("--chunks", type=int, default=0)
    parser.add_argument("--manifest", default="")
    args = parser.parse_args()
    if args.chunks:
        run_chunked(args.output, args.limit, args.offset, args.chunks, args.manifest)
        return
    run(args.output, kind="stock", label="stock", limit=args.limit, offset=args.offset)


if __name__ == "__main__":
    main()
