# yt-digest-web — Lernprojekt Web-Entwicklung

## Hintergrund

Ein kleines Commandline Tool in C# (`yt-digest`) existiert bereits:

```
yt-digest --lang de <youtube video url>
```

Es lädt die Untertitel eines YouTube-Videos und schickt sie an ein LLM, das eine Zusammenfassung als Markdown erstellt.

Dieses Projekt baut eine Web-Oberfläche dafür — als Lernprojekt für Web-Entwicklung.

## Profil

- Erfahrung: C# Desktop-Entwicklung (gut), Web-Entwicklung (kaum), HTML (rudimentär), JavaScript (keins)
- IDE: Rider unter Linux
- Hosting: Azure (50 € Guthaben)

## Entschiedener Stack

- **Backend:** ASP.NET Core — Razor Pages + Minimal API
- **Frontend (Phase 1):** Razor Pages (Server-Rendering, kein JavaScript-Framework)
- **Frontend (Phase 2+):** Weitere Frontends zum Vergleich (z.B. React, Blazor) — nutzen dasselbe Backend-API
- **Abhängigkeiten:** yt-dlp im selben Docker-Container wie die App
- **Deployment:** Docker → Azure Container Apps oder Azure App Service

## Architektur

```
Browser
  └── Razor Pages (HTML Formulare)
        └── REST API  (/api/summarize)
              ├── yt-dlp (Prozess im Container)
              └── LLM API (externer Dienst)
```

Das REST API wird bewusst als eigene Schicht gebaut, damit später verschiedene Frontends verglichen werden können — das Backend bleibt immer gleich.

## Anforderungen

- Login (Auth) — API Key für das LLM wird serverseitig verwaltet, nicht im Browser
- Eingabe: YouTube-URL + Sprache (Dropdown)
- Ausgabe: Markdown-Zusammenfassung angezeigt im Browser
- yt-dlp als Prozess-Abhängigkeit im Container

## API Keys

Gleiche Logik wie in yt-digest — Priorität:
1. `~/.local/share/yt-digest/claude.key` → Anthropic Claude (bevorzugt)
2. `ANTHROPIC_API_KEY` Umgebungsvariable
3. `~/.local/share/yt-digest/deepseek.key` → DeepSeek
4. `DEEPSEEK_API_KEY` Umgebungsvariable

Für Produktion (Docker/Azure): Keys als Umgebungsvariablen übergeben, nicht als Datei.

## Geplante Lernschritte

1. ~~ASP.NET Core Projekt aufsetzen (Razor Pages + Minimal API)~~ ✓
2. SummaryService implementieren (yt-dlp + LLM — Code aus yt-digest wiederverwenden)
3. REST Endpoint `/api/summarize` verdrahten
4. Razor Pages Frontend (Formular → Service → Ergebnis)
5. Login / Authentifizierung
6. Docker-Container bauen (App + yt-dlp)
7. Deployment auf Azure
8. (Optional) Azure OpenAI als LLM-Backend ausprobieren
9. (Optional) Zweites Frontend zum Vergleich (React oder Blazor)
