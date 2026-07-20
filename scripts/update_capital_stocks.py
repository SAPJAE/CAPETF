import argparse

from update_capital_etfs import run


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="data/stocks.raw.json")
    parser.add_argument("--limit", type=int, default=250)
    args = parser.parse_args()
    run(args.output, kind="stock", label="stock", limit=args.limit)


if __name__ == "__main__":
    main()
