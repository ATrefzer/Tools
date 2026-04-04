# backtest

A CLI tool for fetching stock prices and running trading strategy simulations.
Price data is fetched from Yahoo Finance — no API key required.
Output is machine-readable JSON, suitable for piping and further processing.

## Usage

```bash
backtest <ticker> [--buy yyyy-MM-dd] [--sell yyyy-MM-dd] [--prices]
backtest --search <query>
backtest --simulate --in <prices.json> --strategy <name>
backtest --simulate --list
```

### Options

| Option                                     | Description                                                  |
|--------------------------------------------|--------------------------------------------------------------|
| `<ticker>`                                 | Ticker symbol, e.g. `SAP.DE` or `AAPL`                      |
| `--buy yyyy-MM-dd`                         | Buy date. Omit to get the current market price.              |
| `--sell yyyy-MM-dd`                        | Sell date. Defaults to today.                                |
| `--prices`                                 | Output full price series instead of backtest result.         |
| `--search <query>`                         | Search for a ticker by company name.                         |
| `--simulate --in <file> --strategy <name>` | Run a strategy simulation on a price series JSON file.                            |
| `--simulate --list`                        | List all available strategies.                               |

### Examples

```bash
# Current live market price
backtest SAP.DE

# Return from buy date until today
backtest SAP.DE --buy 2020-01-15

# Return with explicit sell date
backtest SAP.DE --buy 2020-01-15 --sell 2024-06-01

# Export price series to a file (input for strategy simulation)
backtest AAPL --prices --buy 2020-01-01 > aapl.json

# List available strategies
backtest --simulate --list

# Run a strategy simulation
backtest --simulate --in aapl.json --strategy BuyAndHold

# Compare multiple simulations visually
backtest --simulate --in aapl.json --strategy BuyAndHold > bah.json
python plot_prices.py bah.json other_strategy.json

# Search for a ticker by company name
backtest --search "Volkswagen"
```

> **Note on dates:** If a date falls on a non-trading day (weekend or public holiday),
> the next available trading day is used automatically.
> The actual date used is reflected in the JSON output under `buyDate` / `sellDate`.

---

## Output Format

All output is written to **stdout** as JSON. Logs and errors go to **stderr**.

### Backtest / current price

```json
{
  "ticker": "SAP.DE",
  "name": "SAP SE",
  "currency": "EUR",
  "buyDate": "2020-01-15",
  "buyPrice": 123.00,
  "sellDate": "2026-04-04",
  "sellPrice": 148.90,
  "absoluteReturn": 25.90,
  "percentReturn": 21.06,
  "annualizedReturn": 3.13,
  "holdingDays": 2271
}
```

When called without `--buy`, only the current market price is returned (`sellDate` and return fields are `null`).

### Price series (`--prices`)

```json
[
  { "date": "2020-01-15", "price": 123.00 },
  { "date": "2020-01-16", "price": 124.50 }
]
```

### Strategy simulation (`--simulate`)

```json
{
  "strategy": "BuyAndHold",
  "initialCapital": 1000,
  "finalCapital": 1423.16,
  "returnPercent": 42.32,
  "steps": [
    {
      "date": "2023-01-02",
      "price": 142.50,
      "action": "Buy",
      "fraction": 1.0,
      "shares": 7.017544,
      "cash": 0,
      "portfolioValue": 1000.00,
      "allTimeHigh": 1000.00,
      "percentFromAllTimeHigh": 0.00
    },
    {
      "date": "2023-06-15",
      "price": 178.20,
      "action": "None",
      "fraction": 0,
      "shares": 7.017544,
      "cash": 0,
      "portfolioValue": 1250.53,
      "allTimeHigh": 1250.53,
      "percentFromAllTimeHigh": 0.00
    }
  ]
}
```

#### Simulation step fields

| Field                   | Description                                                      |
|-------------------------|------------------------------------------------------------------|
| `date`                  | Date of this bar                                                 |
| `price`                 | Closing price                                                    |
| `action`                | Trade executed: `Buy`, `Sell`, or `None`                         |
| `fraction`              | Fraction of cash (Buy) or shares (Sell) traded. `0`..`1`        |
| `shares`                | Shares held after this bar                                       |
| `cash`                  | Uninvested cash after this bar                                   |
| `portfolioValue`        | `cash + shares × price`                                          |
| `allTimeHigh`           | Portfolio value high-water mark up to this bar                   |
| `percentFromAllTimeHigh`| Drawdown from high-water mark in % (`0` at ATH, negative below) |

### Search (`--search`)

```json
[
  { "symbol": "VOW3.DE", "name": "Volkswagen AG", "type": "equity" },
  { "symbol": "VOW.DE",  "name": "Volkswagen AG", "type": "equity" }
]
```

---

## Return Calculation

```
Absolute return   = sell price − buy price
Percentage return = (sell price − buy price) / buy price × 100
Annualized return = ((sell price / buy price) ^ (1 / holding years) − 1) × 100
Holding years     = holding days / 365.25
```

All values are rounded to 2 decimal places.

---

## Strategies

Strategies are discovered automatically — any class in the assembly that implements `IStrategy` is available.

| Strategy     | Description                                                              |
|--------------|--------------------------------------------------------------------------|
| `BuyAndHold` | Buys everything on the first bar, holds until the end. Reference benchmark. |

### Writing a custom strategy

Implement `IStrategy` in the `StockBacktest.Strategies` namespace:

```csharp
internal class MyStrategy : IStrategy
{
    public string Name => "MyStrategy";

    public TradeAction Decide(DecisionContext ctx)
    {
        // ctx.History   — price bars up to today (no look-ahead)
        // ctx.Portfolio — cash, shares, current value before this trade
        if (ctx.History.Count == 1)
            return TradeAction.Buy();        // all-in
        if (ctx.Portfolio.Value < ctx.Portfolio.Cash * 0.9m)
            return TradeAction.Sell();       // stop-loss at -10%
        return TradeAction.None;
    }
}
```

`TradeAction.Buy(fraction)` and `TradeAction.Sell(fraction)` accept an optional fraction (`0`..`1`) to trade only part of the available cash or shares.

---

## Plotting

Requires Python 3 with `matplotlib`.

```bash
# Plot a price series
python plot_prices.py aapl.json

# Plot one or more strategy simulations (overlaid, with buy/sell markers)
python plot_prices.py buyandhold.json mystrategy.json

# Pipe price series directly
backtest AAPL --prices --buy 2020-01-01 | python plot_prices.py
```

---

## Data Source

All data is fetched from **Yahoo Finance** via their public APIs. No API key required.

| Endpoint                                               | Used for                |
|--------------------------------------------------------|-------------------------|
| `query1.finance.yahoo.com/v8/finance/chart/<ticker>`   | Current price & history |
| `query2.finance.yahoo.com/v1/finance/search?q=<query>` | Ticker search           |

---

## Build & Installation

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# Run directly
dotnet run -- SAP.DE --buy 2020-01-15

# Self-contained binary for Linux
dotnet publish -c Release -r linux-x64 -o ./publish/linux

# Self-contained binary for Windows
dotnet publish -c Release -r win-x64 -o ./publish/win
```

Open `backtest.sln` in Rider or Visual Studio for Debug/Release configuration support.
