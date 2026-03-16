using System.Diagnostics;
using SubtitlesParserV2;
using YoutubeDigest.Models;

namespace YoutubeDigest.Services;

/// <summary>
///     Wraps all calls to the external tool yt-dlp.
///     yt-dlp must be available in PATH.
///     Other command lines
///     yt-dlp --list-subs URL
/// </summary>
public class YtDlpService
{
    private readonly string _tempDir;

    public YtDlpService(string tempDir = "temp")
    {
        _tempDir = tempDir;
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    ///     Returns the id and title for a single YouTube video URL.
    /// </summary>
    public async Task<VideoInfo> GetVideoInfoAsync(string videoUrl)
    {
        var args = $"--print \"%(id)s|%(title)s\" \"{videoUrl}\"";
        var output = await RunAsync(args);
        var line = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var parts = line.Split('|', 2);

        return new VideoInfo
        {
            Id = parts.Length > 0 ? parts[0].Trim() : "",
            Title = parts.Length > 1 ? parts[1].Trim() : videoUrl
        };
    }

    /// <summary>
    ///     Returns the latest videos from a YouTube channel. They are returned in the order they appear on the channel.
    /// </summary>
    public async Task<List<VideoInfo>> GetRecentVideosAsync(string channelUrl, int maxVideos = 10)
    {
        // --flat-playlist: no downloads, only metadata
        // --print: define output format (pipe as separator)
        var args = $"--flat-playlist --print \"%(id)s|%(title)s\" --playlist-end {maxVideos} \"{channelUrl}\"";
        var output = await RunAsync(args);

        var videos = new List<VideoInfo>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|', 2); // Max. 2 parts – title may contain |
            if (parts.Length < 2)
            {
                continue;
            }

            videos.Add(new VideoInfo
            {
                Id = parts[0].Trim(),
                Title = parts[1].Trim()
            });
        }

        return videos;
    }

    /// <summary>
    ///     Downloads the subtitles of a video and returns them as cleaned plain text.
    ///     Tries manual subtitles first, then auto-generated for the specified language.
    /// </summary>
    public async Task<string> DownloadSubtitlesAsync(string videoId, string language = "en")
    {
        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
        var outputTemplate = Path.Combine(_tempDir, videoId);

        // Try manual subtitles first
        var args = $"--write-sub --skip-download --sub-lang {language} --output \"{outputTemplate}\" \"{videoUrl}\"";
        await RunAsync(args);

        var vttFile = Directory
            .GetFiles(_tempDir, $"{videoId}*.vtt")
            .FirstOrDefault();

        // If no manual subtitles, try auto-generated
        if (vttFile is null)
        {
            args =
                $"--write-auto-sub --skip-download --sub-lang {language} --output \"{outputTemplate}\" \"{videoUrl}\"";

            await RunAsync(args);

            vttFile = Directory
                .GetFiles(_tempDir, $"{videoId}*.vtt")
                .FirstOrDefault();
        }

        if (vttFile is null)
        {
            Console.WriteLine("No subtitles found for this video. See available sub-titles:");
            args = $"--list.subs \"{videoUrl}\"";

            var result = await RunAsync(args);
            Console.WriteLine(result);
        }

        if (vttFile is null)
        {
            return string.Empty;
        }

        return ParseVtt(vttFile);
    }

    /// <summary>
    ///     Parses a .vtt file into readable plain text.
    /// </summary>
    private static string ParseVtt(string vttFile)
    {
        using var fileStream = File.OpenRead(vttFile);

        // Use a parser. The vtt data comes in various formats.
        var result = SubtitleParser.ParseStream(fileStream);
        if (result is null)
        {
            return string.Empty;
        }

        var merged = result.Subtitles.SelectMany(s => s.Lines);
        return string.Join(" ", merged);
    }

    /// <summary>
    ///     Runs yt-dlp with the specified arguments and returns stdout.
    /// </summary>
    private static async Task<string> RunAsync(string arguments)
    {
        Debug.WriteLine("yt-dlp " + arguments);

        var psi = new ProcessStartInfo
        {
            FileName = "yt-dlp",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
                            ?? throw new InvalidOperationException("yt-dlp could not be started.");

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return output;
    }
}