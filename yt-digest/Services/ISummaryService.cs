using YoutubeDigest.Models;

namespace YoutubeDigest.Services;

/// <summary>
/// Interface for AI summarization services.
/// </summary>
public interface ISummaryService
{
    Task<string> SummarizeAsync(VideoInfo video, string transcript);

    string ModelName { get; }
}
