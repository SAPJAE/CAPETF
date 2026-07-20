import argparse

from update_capital_etfs import run


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="data/stocks.raw.json")
    args = parser.parse_args()
    run(args.output, kind="stock", label="stock")


if __name__ == "__main__":
    main()
