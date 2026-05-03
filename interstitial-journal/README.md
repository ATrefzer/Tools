# Interstitial Journal

A minimalist web app for interstitial journaling.

## Setup in Rider

1. **Open folder:** File → Open → select this folder (`InterstitialJournal/`)
2. **Project is detected automatically** – Rider reads the `.csproj` file
3. **Run:** Green play button in the top right (or `Ctrl+F5`)
4. **Browser opens** automatically at `https://localhost:5000` (or similar)

## Structure

```
InterstitialJournal/
├── Program.cs          ← C# backend code (API)
├── InterstitialJournal.csproj
├── entries.json        ← Created automatically on the first entry
└── wwwroot/
    └── index.html      ← The complete user interface
```

## Features

- ✦ Write an entry → timestamp is set automatically
- Filter entries by day (Today / Yesterday / any date)
- Delete entries (× button appears on hover)
- Everything is stored locally in `entries.json`

## API Endpoints

| Method | URL | Description |
|--------|-----|-------------|
| GET | `/api/entries?date=2024-05-03` | Load entries for a day |
| POST | `/api/entries` | Create a new entry |
| DELETE | `/api/entries/{id}` | Delete an entry |
