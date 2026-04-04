#!/usr/bin/env python3
"""
Usage:
  python plot_prices.py <prices.json>                    # single price series
  backtest SAP.DE --prices --buy 2020-01-15 | python plot_prices.py
  python plot_prices.py sim1.json sim2.json ...          # overlay multiple simulations
"""

import json
import sys
from datetime import datetime
from pathlib import Path
import matplotlib.pyplot as plt
import matplotlib.dates as mdates


def parse_date(s):
    return datetime.fromisoformat(s) if s else None


def is_simulation(data):
    return isinstance(data, dict) and "steps" in data


def load_file(path):
    with open(path) as f:
        return json.load(f)


def load_stdin():
    return json.load(sys.stdin)


def plot_prices(ax, data, label):
    dates  = [parse_date(p["date"]) for p in data]
    prices = [p["price"] for p in data]
    ax.plot(dates, prices, linewidth=1.5, label=label)
    ax.fill_between(dates, prices, alpha=0.08)
    ax.set_ylabel("Price")


def plot_simulation(ax, data, label):
    steps  = data["steps"]
    dates  = [parse_date(s["date"]) for s in steps]
    values = [s["portfolioValue"] for s in steps]

    line, = ax.plot(dates, values, linewidth=1.5, label=label)
    color = line.get_color()

    buys  = [(parse_date(s["date"]), s["portfolioValue"]) for s in steps if s["action"] == "Buy"]
    sells = [(parse_date(s["date"]), s["portfolioValue"]) for s in steps if s["action"] == "Sell"]

    if buys:
        ax.scatter(*zip(*buys),  marker="^", color=color, zorder=5, s=60)
    if sells:
        ax.scatter(*zip(*sells), marker="v", color=color, zorder=5, s=60)

    ret = data.get("returnPercent", "")
    if ret != "":
        line.set_label(f"{label}  ({ret:+.1f}%)")


def make_plot(sources):
    fig, ax = plt.subplots(figsize=(13, 5))

    for path, data in sources:
        label = Path(path).stem if path != "-" else "stdin"
        if is_simulation(data):
            plot_simulation(ax, data, label)
        else:
            plot_prices(ax, data, label)

    ax.xaxis.set_major_formatter(mdates.DateFormatter("%Y-%m"))
    ax.xaxis.set_major_locator(mdates.AutoDateLocator())
    fig.autofmt_xdate()

    ax.set_ylabel("Portfolio value" if any(is_simulation(d) for _, d in sources) else "Price")
    ax.grid(True, alpha=0.3)
    ax.legend()
    plt.tight_layout()
    plt.show()


if __name__ == "__main__":
    args = sys.argv[1:]

    if not args or (len(args) == 1 and args[0] == "-"):
        # Read from stdin
        make_plot([("-", load_stdin())])
    else:
        make_plot([(p, load_file(p)) for p in args])
