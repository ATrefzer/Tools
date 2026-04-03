# backtest

A CLI tool for calculating stock returns over a given holding period.
Price data is fetched from external services — no API key required.
Output is machine-readable JSON, suitable for piping and further processing.

## Usage

```bash
backtest --identifier <ID> --type <type> --buy <date> [--sell <date>]
```

### Options

| Option         | Required | Default | Description                                   |
|----------------|----------|---------|-----------------------------------------------|
| `--identifier` | yes      | —       | Stock identifier (ISIN, WKN or ticker symbol) |
| `--type`       | no       | `ISIN`  | Identifier type: `ISIN` \| `WKN` \| `Ticker`  |
| `--buy`        | yes      | —       | Buy date in format `yyyy-MM-dd`               |
| `--sell`       | no       | today   | Sell date in format `yyyy-MM-dd`              |

### Examples

```bash
# SAP by ISIN, bought in 2020, sold today
backtest --identifier DE0007164600 --type ISIN --buy 2020-01-15

# With explicit sell date
backtest --identifier DE0007164600 --type ISIN --buy 2020-01-15 --sell 2024-06-01

# Ticker directly (no symbol resolution needed)
backtest --identifier SAP.DE --type Ticker --buy 2020-01-15

# US stock by ticker
backtest --identifier AAPL --type Ticker --buy 2019-01-01 --sell 2024-01-01
```

> **Note on dates:** If a date falls on a non-trading day (weekend or public holiday),
> the next available trading day is used automatically.
> The actual date used is reflected in the JSON output under `buyDate` / `sellDate`.

---

## Output Format

All output is written to **stdout** as JSON. Logs and debug information go to **stderr**.

### Successful run

```json
{
  "identifier": "DE0007164600",
  "identifierType": "ISIN",
  "resolvedTicker": "SAP.DE",
  "resolvedName": "SAP SE",
  "currency": "EUR",
  "buyDate": "2020-01-15",
  "buyPrice": 123.00,
  "sellDate": "2026-04-03",
  "sellPrice": 148.90,
  "isSellDateToday": true,
  "absoluteReturn": 25.90,
  "percentReturn": 21.06,
  "annualizedReturn": 3.12,
  "holdingDays": 2269,
  "dataSource": "YahooFinance",
  "calculatedAt": "2026-04-03T10:30:00Z",
  "errors": []
}
```

### Failed run

```json
{
  "identifier": "XX0000000000",
  "identifierType": "ISIN",
  "resolvedTicker": null,
  "resolvedName": null,
  "currency": null,
  "buyDate": null,
  "buyPrice": null,
  "sellDate": null,
  "sellPrice": null,
  "isSellDateToday": false,
  "absoluteReturn": null,
  "percentReturn": null,
  "annualizedReturn": null,
  "holdingDays": null,
  "dataSource": null,
  "calculatedAt": "2026-04-03T10:30:00Z",
  "errors": ["Symbol could not be resolved: no results from OpenFIGI or Yahoo Search for 'XX0000000000'"]
}
```

### Fields

| Field              | Type         | Description                                                   |
|--------------------|--------------|---------------------------------------------------------------|
| `identifier`       | string       | The identifier as provided                                    |
| `identifierType`   | string       | `ISIN`, `WKN` or `Ticker`                                     |
| `resolvedTicker`   | string\|null | Resolved Yahoo Finance ticker symbol                          |
| `resolvedName`     | string\|null | Company name                                                  |
| `currency`         | string\|null | Currency (e.g. `EUR`, `USD`)                                  |
| `buyDate`          | string\|null | Actual buy date used (`yyyy-MM-dd`)                           |
| `buyPrice`         | number\|null | Closing price on the buy date                                 |
| `sellDate`         | string\|null | Actual sell date used (`yyyy-MM-dd`)                          |
| `sellPrice`        | number\|null | Closing price on the sell date                                |
| `isSellDateToday`  | bool         | `true` if no `--sell` was given and the date equals today     |
| `absoluteReturn`   | number\|null | Absolute gain or loss                                         |
| `percentReturn`    | number\|null | Percentage gain or loss                                       |
| `annualizedReturn` | number\|null | Annualized return (CAGR) in %                                 |
| `holdingDays`      | number\|null | Holding period in calendar days                               |
| `dataSource`       | string\|null | Price source: `YahooFinance`, `Stooq` or `YahooFinance/Stooq` |
| `calculatedAt`     | string       | Calculation timestamp (ISO 8601, UTC)                         |
| `errors`           | string[]     | Error messages; empty on success                              |

### Exit Codes

| Code | Meaning                                                                          |
|------|----------------------------------------------------------------------------------|
| `0`  | Success (even if `errors` contains warnings, as long as price data is available) |
| `1`  | Fatal error — no price data could be retrieved                                   |

---

## External Services

No API keys are required. The following services are used:

### Symbol Resolution (ISIN/WKN → Ticker)

#### 1. OpenFIGI *(primary)*

- **URL:** `https://api.openfigi.com/v3/mapping`
- **Rate limit:** 25 requests/minute (without API key)
- Resolves the ticker symbol and exchange from an ISIN or WKN.
  For German ISINs (prefix `DE`), XETRA is preferred (e.g. `SAP.DE`);
  for US ISINs, NYSE/NASDAQ (no suffix); and so on.

#### 2. Yahoo Finance Search *(fallback)*

- **URL:** `https://query2.finance.yahoo.com/v1/finance/search?q=<ID>`
- Used when OpenFIGI returns no result.
- Returns the first match from the full-text search.

### Price Data

#### 1. Yahoo Finance *(primary)*

- **URL:** `https://query1.finance.yahoo.com/v8/finance/chart/<ticker>`
- Provides daily closing prices (`interval=1d`) for a date range.
- No API key required.

#### 2. Stooq *(fallback)*

- **URL:** `https://stooq.com/q/d/l/?s=<ticker>&d1=<from>&d2=<to>&i=d`
- CSV download, no API key required.
- Activated when Yahoo Finance returns HTTP 429 or empty data.

---

## Return Calculation

All values are rounded to 2 decimal places.

```
Absolute return    = sell price − buy price
Percentage return  = (sell price − buy price) / buy price × 100
Annualized return (CAGR) = ((sell price / buy price) ^ (1 / holding years) − 1) × 100
Holding years      = holding days / 365.25
```

---

## Build & Installation

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# Run directly (debug build)
dotnet run -- --identifier SAP.DE --type Ticker --buy 2020-01-15

# Publish a release binary (platform-dependent)
dotnet publish -c Release -o ./publish

# Self-contained binary for Linux (no .NET runtime required on target)
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish/linux

# Self-contained binary for Windows
dotnet publish -c Release -r win-x64 --self-contained -o ./publish/win
```
