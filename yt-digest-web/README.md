# yt-digest-web

A small web front end for the `yt-digest` command line tool. Enter a YouTube URL, pick the
subtitle language, and get a Markdown summary of the video rendered in the browser.

## Architecture

```
Browser
  └── Razor Pages (Login, Index)          cookie authentication
        └── YtDigestService               starts yt-digest as an external process
              └── yt-digest (CLI, in PATH)
                    ├── yt-dlp            downloads the subtitles
                    └── LLM API           creates the summary (Claude / DeepSeek)

REST API: POST /api/summarize  { "url": "...", "lang": "de" }  → { "summary": "..." }
```

- **ASP.NET Core (.NET 10)** – Razor Pages for the UI plus a Minimal API endpoint. Both use the
  same `YtDigestService`, so other front ends can be added later on top of the REST API.
- **Authentication** – a single shared password (`Auth:Password`). A successful login issues an
  auth cookie; all pages except `/Login` and the API endpoint require it.
- **YtDigestService** (`Services/YtDigestService.cs`) – runs
  `yt-digest --lang <lang> --summary-lang German "<url>"` and returns its stdout.
- **Markdig** – converts the Markdown summary to HTML for display.
- **API keys** are never handled by the web app. They are resolved by `yt-digest` itself
  (key files or `ANTHROPIC_API_KEY` / `DEEPSEEK_API_KEY` environment variables).

## Prerequisites

- .NET 10 SDK
- `yt-digest` in `PATH`, fully working on its own (including `yt-dlp` and an LLM API key)

## Password

The login password is read from the configuration key `Auth:Password`. `appsettings.json`
contains the default `123`, which is only meant for local development. Override it with one of:

```powershell
# Environment variable (double underscore = section separator)
$env:Auth__Password = "my-secret"
dotnet run
```

```powershell
# Command line argument
dotnet run -- --Auth:Password="my-secret"
```

## Running locally

```powershell
cd yt-digest-web
dotnet run
```

Uses the launch profile from `Properties/launchSettings.json` (http://localhost:5065).

## Running with access from the local network

By default Kestrel only listens on `localhost`. To reach the app from other devices, bind to
all network interfaces:

```powershell
cd yt-digest-web
$env:Auth__Password = "my-secret"
dotnet run --urls "http://0.0.0.0:5000"
```

Or with a published build:

```powershell
dotnet publish -c Release -o out
$env:Auth__Password = "my-secret"
.\out\yt-digest-web.exe --urls "http://0.0.0.0:5000"
```

Then open `http://<machine-ip>:5000` from another device (find the IP with `ipconfig`).

### Windows firewall rule

Windows blocks incoming connections by default. Create an inbound rule for port 5000
(run PowerShell **as Administrator**):

```powershell
New-NetFirewallRule -DisplayName "yt-digest-web" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Private
```

`-Profile Private` limits the rule to networks marked as private (e.g. your home network).
Remove the rule again with:

```powershell
Remove-NetFirewallRule -DisplayName "yt-digest-web"
```

> **Note:** The app runs over plain HTTP, so the password is sent unencrypted. Only expose it
> in a trusted network and always set your own password instead of the default.
