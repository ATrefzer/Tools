using System.Diagnostics;
using YoutubeDigest.Models;
using YoutubeDigest.Services;

namespace YoutubeDigest;

internal class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Single-video mode: yt-digest <url> [--lang <language>]
            var (videoUrl, language) = ParseCommandLine(args);
            if (videoUrl != null)
            {
                await PrintSingleVideoSummary(videoUrl, language);
            }
            else
            {
                var digestPath = await CreateDigestFile();
                

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

    private static async Task<string> CreateDigestFile()
    {
        var channels = await LoadChannelsAsync();
        if (channels.Count == 0)
        {
            return string.Empty;
        }

        var summaryService = SummaryServiceFactory.Create();
        var store = new SummaryStore(AppPaths.ProcessedVideosFile);
        var ytService = new YtDlpService();
        var digest = new DigestBuilder();

        Console.WriteLine("YouTube Digest started");
        Console.WriteLine($"\tAI       : {summaryService.ModelName}");
        Console.WriteLine($"\tChannels : {channels.Count}");
        Console.WriteLine();

        foreach (var channel in channels)
        {
            await ProcessChannelAsync(channel, ytService, summaryService, store, digest);
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

    private static (string? videoUrl, string language) ParseCommandLine(string[] args)
    {
        var langIndex = Array.IndexOf(args, "--lang");
        var language = langIndex >= 0 && langIndex + 1 < args.Length ? args[langIndex + 1] : "en";
        var videoUrl =
            args.FirstOrDefault(a => a.StartsWith("http://") || a.StartsWith("https://") || a.StartsWith("www."));
        return (videoUrl, language);
    }

    private static async Task PrintSingleVideoSummary(string videoUrl, string language)
    {
        // Info messages are written to stderr to keep stdout clean for potential redirection.

        var summaryService = SummaryServiceFactory.Create();


        var ytDlp = new YtDlpService();

        await Console.Error.WriteLineAsync("Fetching video info...");
        var video = await ytDlp.GetVideoInfoAsync(videoUrl);

        await Console.Error.WriteLineAsync($"Downloading subtitles ({language})...");
        var transcript = await ytDlp.DownloadSubtitlesAsync(video.Id, language);
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
        YtDlpService ytDlp,
        ISummaryService summaryService,
        SummaryStore store,
        DigestBuilder digest)
    {
        Console.WriteLine($"Channel: {channel.Url} ({channel.Language})");
        Console.WriteLine($"\tFetching last {Constants.MaxVideosPerChannel} videos...");

        var allVideos = await ytDlp.GetRecentVideosAsync(channel.Url, Constants.MaxVideosPerChannel);
        var newVideos = allVideos
            .Where(v => !store.IsProcessed(v.Id))
            .Take(Constants.MaxVideosPerChannel)
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