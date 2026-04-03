# backtest

A CLI tool for calculating stock returns over a given holding period.
Price data is fetched from Yahoo Finance — no API key required.
Output is machine-readable JSON, suitable for piping and further processing.

## Usage

```bash
backtest <ticker> [--buy yyyy-MM-dd] [--sell yyyy-MM-dd] [--prices]
backtest --search <query>
```

### Options

| Option             | Required | Default | Description                                      |
|--------------------|----------|---------|--------------------------------------------------|
| `<ticker>`         | yes*     | —       | Ticker symbol, e.g. `SAP.DE` or `AAPL`          |
| `--buy yyyy-MM-dd` | no       | —       | Buy date. Omit to get the current market price.  |
| `--sell yyyy-MM-dd`| no       | today   | Sell date.                                       |
| `--prices`         | no       | —       | Output full price series instead of backtest result. |
| `--search <query>` | —        | —       | Search for a ticker by company name. Standalone. |

*Not required when using `--search`.

### Examples

```bash
# Current market price
backtest SAP.DE

# Backtest from buy date until today
backtest SAP.DE --buy 2020-01-15

# Backtest with explicit sell date
backtest SAP.DE --buy 2020-01-15 --sell 2024-06-01

# Full price series for plotting, written to a file
backtest SAP.DE --prices --buy 2020-01-15 > sap_prices.json

# Search for a ticker by company name
backtest --search "Volkswagen"
```

> **Note on dates:** If a date falls on a non-trading day (weekend or public holiday),
> the next available trading day is used automatically.
> The actual date used is reflected in the JSON output under `buyDate` / `sellDate`.

---

## Output Format

All output is written to **stdout** as JSON. Logs and debug information go to **stderr**.

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
  { "date": "2020-01-16", "price": 124.50 },
  ...
]
```

### Search (`--search`)

```json
[
  { "symbol": "VOW3.DE", "name": "Volkswagen AG", "type": "equity" },
  { "symbol": "VOW.DE",  "name": "Volkswagen AG", "type": "equity" }
]
```

### Fields — backtest result

| Field              | Type         | Description                                      |
|--------------------|--------------|--------------------------------------------------|
| `ticker`           | string       | Ticker symbol as provided                        |
| `name`             | string\|null | Company name                                     |
| `currency`         | string\|null | Currency (e.g. `EUR`, `USD`)                     |
| `buyDate`          | string\|null | Actual buy date used (`yyyy-MM-dd`)              |
| `buyPrice`         | number\|null | Price on the buy date                            |
| `sellDate`         | string\|null | Actual sell date used (`yyyy-MM-dd`)             |
| `sellPrice`        | number\|null | Price on the sell date                           |
| `absoluteReturn`   | number\|null | Absolute gain or loss                            |
| `percentReturn`    | number\|null | Percentage gain or loss                          |
| `annualizedReturn` | number\|null | Annualized return (CAGR) in %                    |
| `holdingDays`      | number\|null | Holding period in calendar days                  |

### Exit Codes

| Code | Meaning                          |
|------|----------------------------------|
| `0`  | Success                          |
| `1`  | Fatal error (logged to stderr)   |

---

## Return Calculation

All values are rounded to 2 decimal places.

```
Absolute return   = sell price − buy price
Percentage return = (sell price − buy price) / buy price × 100
Annualized return (CAGR) = ((sell price / buy price) ^ (1 / holding years) − 1) × 100
Holding years     = holding days / 365.25
```

---

## Data Source

All data is fetched from **Yahoo Finance** via their public chart and search APIs.
No API key is required.

| Endpoint                                                  | Used for                    |
|-----------------------------------------------------------|-----------------------------|
| `query1.finance.yahoo.com/v8/finance/chart/<ticker>`      | Current price & history     |
| `query2.finance.yahoo.com/v1/finance/search?q=<query>`    | Ticker search               |

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
