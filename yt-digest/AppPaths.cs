namespace YoutubeDigest;

internal static class AppPaths
{
    public static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "yt-digest");

    public static readonly string ProcessedVideosFile = Path.Combine(AppDataDir, "processed_videos.json");
    public static readonly string SummariesDir = Path.Combine(AppDataDir, "summaries");
    public static readonly string DigestsDir = Path.Combine(AppDataDir, "digests");
    public static readonly string KeyFile = Path.Combine(AppDataDir, "key.txt");
    public static readonly string ChannelsFile = Path.Combine(AppDataDir, "channels.txt");

    static AppPaths()
    {
        Directory.CreateDirectory(AppDataDir);
    }

    public static readonly string TempDir = Path.Combine(AppDataDir, "Temp");
}
