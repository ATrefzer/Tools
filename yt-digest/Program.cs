using System.Diagnostics;
using YoutubeDigest.Models;
using YoutubeDigest.Services;

namespace YoutubeDigest;

internal class Program
{
    public static async Task Main(string[] args)
    {
        // Simple format
        //var text = VttParser.ParseFile("C:\\Users\\ATrefzer\\AppData\\Local\\yt-digest\\temp\\QOE_uXsAc4I.de-DE.vtt");

        // Karaoke format
        //text = VttParser.ParseFile("C:\\Users\\ATrefzer\\AppData\\Local\\yt-digest\\summaries\\a-Tq53g2Ows.de.vtt");

        try
        {
            var options = CommandLineOptions.Parse(args);

            if (options.ShowHelp)
            {
                PrintHelp();
                return;
            }

            if (options.VideoUrl != null)
            {
                await PrintSingleVideoSummary(options);
            }
            else
            {
                var digestPath = await CreateDigestFile(options);

                if (!string.IsNullOrEmpty(digestPath))
                {
                    Console.WriteLine($"Digest saved to: {digestPath}");
                    OpenFile(digestPath);
                }
            }
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.ToString());
        }
    }

    private static async Task<string> CreateDigestFile(CommandLineOptions options)
    {
        var channels = await LoadChannelsAsync();
        if (channels.Count == 0)
        {
            return string.Empty;
        }

        var summaryService = SummaryServiceFactory.Create(options.SummaryLang);
        var store = new SummaryStore(AppPaths.ProcessedVideosFile);
        var ytService = new YtDlpService(AppPaths.TempDir);
        var digest = new DigestBuilder();

        Console.WriteLine("YouTube Digest started");
        Console.WriteLine($"\tAI         : {summaryService.ModelName}");
        Console.WriteLine($"\tChannels   : {channels.Count}");
        Console.WriteLine($"\tMax videos : {options.MaxVideos}");
        Console.WriteLine();

        foreach (var channel in channels)
        {
            await ProcessChannelAsync(channel, options.MaxVideos, ytService, summaryService, store, digest);
        }

        var digestPath = await digest.SaveAsync();

        Console.WriteLine($"Done! {digest.Count} videos processed.");

        return digestPath;
    }

    public static void OpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not open digest file: {ex.Message}");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            YouTube Digest - Summarize YouTube videos using AI

            USAGE
              yt-digest              Digest mode. Process all channels from channels.txt.
                                     Output to markdown file.
              yt-digest <url>        Summarize a single video.
                                     Output to console.
              yt-digest --help       Show this help

            OPTIONS
              --lang <code>          Subtitle language to download (default: en)
                                     Examples: en, de
              --summary-lang <lang>  Language for the generated summary
                                     Defaults to the transcript language
                                     Examples: German, English etc.
              --max-videos <n>       Max videos to fetch per channel in digest mode
                                     Default: 2

            EXAMPLES
              yt-digest
              yt-digest https://www.youtube.com/watch?v=VIDEO_ID
              yt-digest https://www.youtube.com/watch?v=VIDEO_ID --lang de
              yt-digest https://www.youtube.com/watch?v=VIDEO_ID --summary-lang German
              yt-digest --summary-lang German
              yt-digest --max-videos 5
              yt-digest https://... --lang en --summary-lang German > summary.md

            CONFIGURATION
              All files are stored in the app data directory:
                Windows : %LOCALAPPDATA%\yt-digest\
                Linux   : ~/.local/share/yt-digest/
                macOS   : ~/Library/Application Support/yt-digest/

              channels.txt  Channels to process in digest mode (one URL per line, optional ,lang suffix)
              claude.key    Anthropic Claude API key
              deepseek.key  DeepSeek API key

            AI BACKEND PRIORITY
              1. claude.key / ANTHROPIC_API_KEY
              2. deepseek.key / DEEPSEEK_API_KEY
              3. Ollama (local fallback, http://localhost:11434)
            """);
    }

    private static async Task PrintSingleVideoSummary(CommandLineOptions options)
    {
        // Info messages are written to stderr to keep stdout clean for potential redirection.

        var summaryService = SummaryServiceFactory.Create(options.SummaryLang);
        var ytDlp = new YtDlpService(AppPaths.TempDir);

        await Console.Error.WriteLineAsync("Fetching video info...");
        var video = await ytDlp.GetVideoInfoAsync(options.VideoUrl!);

        await Console.Error.WriteLineAsync($"Downloading subtitles ({options.SubtitleLang})...");
        var transcript = await ytDlp.DownloadSubtitlesAsync(video.Id, options.SubtitleLang);
        if (string.IsNullOrWhiteSpace(transcript))
        {
            Console.WriteLine("No subtitles available for this video.");
            return;
        }

        await Console.Error.WriteLineAsync("Generating summary...");
        var summary = await summaryService.SummarizeAsync(video, transcript);

        // Redirect to a file if needed.
        await Console.Out.WriteLineAsync(summary);
    }

    private static async Task<List<ChannelConfig>> LoadChannelsAsync()
    {
        if (!File.Exists(AppPaths.ChannelsFile))
        {
            await File.WriteAllTextAsync(AppPaths.ChannelsFile,
                "# Add one YouTube channel URL per line.\n# Example:\n# https://www.youtube.com/@SomeChannel/videos[,subtitle-lang]\n");
            Console.WriteLine($"Created channels file: {AppPaths.ChannelsFile}");
            Console.WriteLine("\tAdd YouTube channel URLs and run again.");
            return [];
        }

        var channels = (await File.ReadAllLinesAsync(AppPaths.ChannelsFile))
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#"))
            .Select(ChannelConfig.Parse)
            .ToList();

        if (channels.Count == 0)
        {
            Console.WriteLine($"Error: No channels configured in {AppPaths.ChannelsFile}");
        }

        return channels;
    }

    private static async Task ProcessChannelAsync(
        ChannelConfig channel,
        int maxVideos,
        YtDlpService ytDlp,
        ISummaryService summaryService,
        SummaryStore store,
        DigestBuilder digest)
    {
        Console.WriteLine($"Channel: {channel.Url} ({channel.Language})");
        Console.WriteLine($"\tFetching last {maxVideos} videos...");

        var allVideos = await ytDlp.GetRecentVideosAsync(channel.Url, maxVideos);
        var newVideos = allVideos
            .Where(v => !store.IsProcessed(v.Id))
            .Take(maxVideos)
            .ToList();

        Console.WriteLine($"\t{newVideos.Count} new videos to process.\n");

        if (newVideos.Count > 0)
        {
            digest.AddChannelHeader(channel.Name);
        }

        foreach (var video in newVideos)
        {
            await ProcessVideoAsync(video, channel.Language, ytDlp, summaryService, store, digest);
        }
    }

    private static async Task ProcessVideoAsync(
        VideoInfo video,
        string language,
        YtDlpService ytDlp,
        ISummaryService summaryService,
        SummaryStore store,
        DigestBuilder digest)
    {
        Console.WriteLine($"\t\tProcessing: {video.Title}");

        var transcript = await ytDlp.DownloadSubtitlesAsync(video.Id, language);
        if (string.IsNullOrWhiteSpace(transcript))
        {
            Console.WriteLine("\t\t(!) No subtitles available – skipping.\n");
            store.MarkProcessed(video.Id, video.Title);
            return;
        }

        Console.WriteLine($"\t\tTranscript: {transcript.Length:N0} characters");
        Console.WriteLine("\t\tGenerating summary...");

        var summary = await summaryService.SummarizeAsync(video, transcript);

        // Save individual summary
        Directory.CreateDirectory(AppPaths.SummariesDir);
        var outputPath = Path.Combine(AppPaths.SummariesDir, $"{video.Id}.md");
        await File.WriteAllTextAsync(outputPath, summary);

        // Add to digest and mark as processed
        digest.Add(summary);
        store.MarkProcessed(video.Id, video.Title);

        Console.WriteLine($"\t\tSaved to: {outputPath}\n");
    }
}
