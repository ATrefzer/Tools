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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly string _lastRunPath;
    private readonly Dictionary<string, ProcessedVideo> _processed;

    public SummaryStore(string filePath)
    {
        _filePath = filePath;
        _lastRunPath = Path.ChangeExtension(filePath, ".lastrun");
        _processed = Load();
    }

    /// <summary>
    ///     Returns the timestamp of the last run, or null if never run before.
    /// </summary>
    public DateTime? GetLastRun()
    {
        if (!File.Exists(_lastRunPath))
            return null;

        var text = File.ReadAllText(_lastRunPath).Trim();
        if (DateTime.TryParse(text, out var lastRun))
            return lastRun;

        return null;
    }

    /// <summary>
    ///     Updates the last run timestamp to now.
    /// </summary>
    public void UpdateLastRun()
    {
        File.WriteAllText(_lastRunPath, DateTime.UtcNow.ToString("O"));
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
        return JsonSerializer.Deserialize<Dictionary<string, ProcessedVideo>>(json)
               ?? new Dictionary<string, ProcessedVideo>();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_processed, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
