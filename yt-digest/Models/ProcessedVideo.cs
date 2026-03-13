namespace YoutubeDigest.Models;

/// <summary>
/// Stores metadata for an already processed video.
/// </summary>
public record ProcessedVideo
{
    public string   Id          { get; init; } = "";
    public string   Title       { get; init; } = "";
    public DateTime ProcessedAt { get; init; }
}