namespace YoutubeDigest.Models;

/// <summary>
/// Represents a YouTube video as returned by yt-dlp.
/// </summary>
public record VideoInfo
{
    public string Id    { get; init; } = "";
    public string Title { get; init; } = "";
}

/// <summary>
/// Represents a channel configuration from channels.txt.
/// Format: URL,lang (e.g. https://www.youtube.com/@NDC/videos,en)
/// </summary>
public record ChannelConfig
{
    public string Url      { get; init; } = "";
    public string Language { get; init; } = "en"; // Default to English
    public string Name     { get; init; } = "";   // Extracted from URL

    public static ChannelConfig Parse(string line)
    {
        var parts = line.Split(',', 2);
        var url = parts[0].Trim();

        return new ChannelConfig
        {
            Url      = url,
            Language = parts.Length > 1 ? parts[1].Trim() : "en",
            Name     = ExtractChannelName(url)
        };
    }

    private static string ExtractChannelName(string url)
    {
        // Extract channel name from URLs like:
        // https://www.youtube.com/@NDC/videos -> NDC
        // https://www.youtube.com/c/ChannelName/videos -> ChannelName
        // https://www.youtube.com/channel/UC.../videos -> UC...

        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            return url;

        var name = segments[0];

        // Remove @ prefix if present
        if (name.StartsWith('@'))
            name = name[1..];

        // Skip "c" or "channel" prefixes
        if ((name == "c" || name == "channel") && segments.Length > 1)
            name = segments[1];

        return name;
    }
}