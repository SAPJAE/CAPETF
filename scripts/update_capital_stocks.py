import argparse
import json
from pathlib import Path

from update_capital_etfs import run


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="data/stocks.raw.json")
    parser.add_argument("--limit", type=int, default=250)
    parser.add_argument("--offset", type=int, default=0)
    parser.add_argument("--chunks", type=int, default=0)
    parser.add_argument("--manifest", default="")
    args = parser.parse_args()
    if args.chunks:
        output = Path(args.output)
        output.parent.mkdir(parents=True, exist_ok=True)
        chunk_files = []
        for chunk_index in range(args.chunks):
            chunk_output = output.parent / f"{output.stem}-{chunk_index:03d}{output.suffix}"
            offset = args.offset + (chunk_index * args.limit)
            run(
                str(chunk_output),
                kind="stock",
                label="stock",
                limit=args.limit,
                offset=offset,
                metadata={"chunkIndex": chunk_index, "chunkCount": args.chunks, "offset": offset, "limit": args.limit},
            )
            chunk_files.append(str(chunk_output).replace("\\", "/").replace(".raw.json", ".enc.json"))
        if args.manifest:
            Path(args.manifest).write_text(
                json.dumps({"chunkCount": args.chunks, "chunkFiles": chunk_files}, indent=2),
                encoding="utf-8",
            )
        return
    run(args.output, kind="stock", label="stock", limit=args.limit, offset=args.offset)


if __name__ == "__main__":
    main()
