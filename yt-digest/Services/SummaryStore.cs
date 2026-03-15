using System.Text.Json;
using YoutubeDigest.Models;

namespace YoutubeDigest.Services;

/// <summary>
///     Saves and loads the processing status of all videos.
///     Prevents the same video from being processed multiple times.
///     Also tracks when the tool was last run.
/// </summary>
public class SummaryStore
{
    private static readonly AppJsonContext JsonContext = new(new JsonSerializerOptions { WriteIndented = true });

    private readonly string _filePath;
    private readonly Dictionary<string, ProcessedVideo> _processed;

    public SummaryStore(string filePath)
    {
        _filePath = filePath;
        _processed = Load();
    }
    

    /// <summary>
    ///     Checks if a video has already been processed.
    /// </summary>
    public bool IsProcessed(string videoId)
    {
        return _processed.ContainsKey(videoId);
    }

    /// <summary>
    ///     Marks a video as processed and saves the store.
    /// </summary>
    public void MarkProcessed(string videoId, string title)
    {
        _processed[videoId] = new ProcessedVideo
        {
            Id = videoId,
            Title = title,
            ProcessedAt = DateTime.UtcNow
        };
        Save();
    }

    private Dictionary<string, ProcessedVideo> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<string, ProcessedVideo>();
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize(json, JsonContext.DictionaryStringProcessedVideo)
               ?? new Dictionary<string, ProcessedVideo>();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_processed, JsonContext.DictionaryStringProcessedVideo);
        File.WriteAllText(_filePath, json);
    }
}
