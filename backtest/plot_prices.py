#!/usr/bin/env python3
"""
Usage:
  python plot_prices.py <prices.json>
  backtest SAP.DE --prices --buy 2020-01-15 | python plot_prices.py
"""

import json
import sys
from datetime import datetime
import matplotlib.pyplot as plt
import matplotlib.dates as mdates


def load(source):
    if source == "-" or not sys.stdin.isatty():
        data = json.load(sys.stdin)
    else:
        with open(source) as f:
            data = json.load(f)
    dates  = [datetime.strptime(p["date"], "%Y-%m-%d") for p in data]
    prices = [p["price"] for p in data]
    return dates, prices


def plot(dates, prices, title="Price History"):
    fig, ax = plt.subplots(figsize=(12, 5))

    ax.plot(dates, prices, linewidth=1.5)
    ax.fill_between(dates, prices, alpha=0.1)

    ax.xaxis.set_major_formatter(mdates.DateFormatter("%Y-%m"))
    ax.xaxis.set_major_locator(mdates.AutoDateLocator())
    fig.autofmt_xdate()

    ax.set_title(title)
    ax.set_ylabel("Price")
    ax.grid(True, alpha=0.3)

    plt.tight_layout()
    plt.show()


if __name__ == "__main__":
    source = sys.argv[1] if len(sys.argv) > 1 else "-"
    dates, prices = load(source)
    title = sys.argv[1] if len(sys.argv) > 1 else "Price History"
    plot(dates, prices, title)
