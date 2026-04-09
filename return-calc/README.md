# return-calc

A CLI tool for fetching stock prices and calculating the return between two dates.
Price data is fetched from Yahoo Finance — no API key required.
Output is machine-readable JSON, suitable for piping and further processing.

## Usage

```bash
return-calc <ticker> [--buy yyyy-MM-dd] [--sell yyyy-MM-dd] [--prices]
return-calc --search <query>
```

### Options

| Option                                     | Description                                                  |
|--------------------------------------------|--------------------------------------------------------------|
| `<ticker>`                                 | Ticker symbol, e.g. `SAP.DE` or `AAPL`                       |
| `--buy yyyy-MM-dd`                         | Buy date. Omit to get the current market price.              |
| `--sell yyyy-MM-dd`                        | Sell date. Defaults to today.                                |
| `--prices`                                 | Output full price series instead of return calculation       |
| `--search <query>`                         | Search for a ticker by company name.                         |


### Examples

```bash
# Current live market price
return-calc SAP.DE

# Return from buy date until today
return-calc SAP.DE --buy 2020-01-15

# Return with explicit sell date
return-calc SAP.DE --buy 2020-01-15 --sell 2024-06-01

# Export price series to a file
return-calc AAPL --prices --buy 2020-01-01 > aapl.json

# Search for a ticker by company name
return-calc --search "Volkswagen"
```

> **Note on dates:** If a date falls on a non-trading day (weekend or public holiday),
> the next available trading day is used automatically.
> The actual date used is reflected in the JSON output under `buyDate` / `sellDate`.

---

## Output Format

All output is written to **stdout** as JSON. Logs and errors go to **stderr**.

### return-calc / current price

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


`TradeAction.Buy(fraction)` and `TradeAction.Sell(fraction)` accept an optional fraction (`0`..`1`) to trade only part of the available cash or shares.

---

## Plotting

Requires Python 3 with `matplotlib`.

```bash
# Plot a price series
python plot_prices.py aapl.json

# Pipe price series directly
return-calc AAPL --prices --buy 2020-01-01 | python plot_prices.py
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

Open `return-calc.sln` in Rider or Visual Studio for Debug/Release configuration support.
