using System.Diagnostics;
using System.Text;

namespace YoutubeDigest.Services;

/// <summary>
///     Builds and saves the combined digest of all summaries.
/// </summary>
public class DigestBuilder
{
    private readonly StringBuilder _content = new();
    private readonly DateTime _timestamp;

    public DigestBuilder()
    {
        _timestamp = DateTime.Now;
        _content.AppendLine("[TOC]");
        _content.AppendLine($"# YouTube Digest – {_timestamp:yyyy-MM-dd HH:mm}");
        _content.AppendLine();
    }

    public int Count { get; private set; }

    public void AddChannelHeader(string channelName)
    {
        _content.AppendLine($"# {channelName}");
        _content.AppendLine();
    }

    public void Add(string summary)
    {
        _content.AppendLine(summary);
        _content.AppendLine();
        _content.AppendLine("---");
        _content.AppendLine();
        Count++;
    }

    /// <summary>
    ///     Saves the digest to a file and optionally opens it.
    ///     Returns the file path, or null if nothing was written.
    /// </summary>
    public async Task<string> SaveAsync()
    {
        if (Count == 0)
        {
            return string.Empty;
        }

        Directory.CreateDirectory("digests");
        var path = Path.Combine("digests", $"{_timestamp:yyyy-MM-dd_HH-mm}.md");
        await File.WriteAllTextAsync(path, _content.ToString());


        return path;
    }
}