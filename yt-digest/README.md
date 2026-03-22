# YouTube Digest

[TOC]

Automatically summarize YouTube videos using yt-dlp and AI (Claude, DeepSeek, or Ollama).

Note: Summary output is in German, you can change this in class Prompt.

## Prerequisites

| Tool     | Installation                                                 |
| -------- | ------------------------------------------------------------ |
| .NET SDK | https://dotnet.microsoft.com/download                        |
| yt-dlp   | Needs to be in the search path.<br />`winget install yt-dlp` or https://github.com/yt-dlp/yt-dlp |

## Usage

### Digest mode (default)

Processes all channels from `channels.txt` and creates a combined digest file:

```bash
yt-digest.exe
```

The tool will:

1. Reads channels from `channels.txt`
2. Fetch the most recent videos per channel
3. Skip already-processed videos
4. Download subtitles (manual first, then auto-generated)
5. Generate AI summaries
6. Save everything as Markdown
7. Open the digest automatically

#### Output

All data is stored in the platform app data directory:

| Platform | Path |
| -------- | ---- |
| Linux    | `~/.local/share/yt-digest/` |
| Windows  | `%LOCALAPPDATA%\yt-digest\` |
| macOS    | `~/Library/Application Support/yt-digest/` |

```
yt-digest/
├── summaries/            ← Individual summaries (one per video)
├── digests/              ← Combined digests per run (named by date)
├── temp/                 ← Temporary .vtt subtitle files (can be deleted)
├── processed_videos.json ← Record of already-processed videos
├── channels.txt          ← Channel configuration
├── claude.key            ← Claude API key (optional)
└── deepseek.key          ← DeepSeek API key (optional)
```

Videos without subtitles are recorded as processed and will not be retried on subsequent runs.

#### Configuration: channels.txt

The file is created automatically on first run. Edit it to contain all channels included in the digest.

One channel per line, with an optional language code:

```
# Format: URL,language  (en or de)
# Language is optional, defaults to en

https://www.youtube.com/@NDC/videos,en

```

#### Other settings

In `Constants.cs`:

```csharp
const int MaxVideosPerChannel = 2;  // How many recent videos to check per channel
```

### Single-video mode

Summarize one specific video and print the result as Markdown:

```bash
yt-digest.exe https://www.youtube.com/watch?v=VIDEO_ID
```

With an explicit language for the subtitles:

```bash
yt-digest.exe https://www.youtube.com/watch?v=VIDEO_ID --lang de
```

The summary is written to stdout, so it can be piped or redirected:

```bash
yt-digest.exe https://www.youtube.com/watch?v=VIDEO_ID > summary.md
```

> Progress messages are written to stderr and do not appear in the redirected output.

## AI Backend

The tool supports three AI backends. Priority order:

1. **Claude** — `claude.key` file or `ANTHROPIC_API_KEY` env var
2. **DeepSeek** — `deepseek.key` file or `DEEPSEEK_API_KEY` env var
3. **Ollama** — automatic fallback (no key required)

### Option A: Claude

1. Create an account: https://console.anthropic.com/
2. Add credits and create an API key

**Option 1: claude.key (recommended)**

Create a file named `claude.key` in the app data directory (see Output section) containing only the API key:
```
sk-ant-api03-...
```

**Option 2: Environment variable**
```bash
# Windows (CMD)
set ANTHROPIC_API_KEY=sk-ant-...

# Linux
export ANTHROPIC_API_KEY=sk-ant-...
```

### Option B: DeepSeek

Good quality summaries, very low cost.

1. Create an account: https://platform.deepseek.com/
2. Add credits and create an API key

**Option 1: deepseek.key (recommended)**

Create a file named `deepseek.key` in the app data directory containing only the API key:
```
sk-...
```

**Option 2: Environment variable**
```bash
# Windows (CMD)
set DEEPSEEK_API_KEY=sk-...

# Linux
export DEEPSEEK_API_KEY=sk-...
```

### Option C: Ollama (free, local)

Runs entirely offline, requires decent hardware (8 GB+ RAM).

1. Install Ollama: https://ollama.com
2. Pull a model:
```bash
ollama pull llama3.2
```
3. Ollama runs automatically in the background (http://localhost:11434)

> If no API key is configured, the tool falls back to Ollama automatically.
