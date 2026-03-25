namespace YoutubeDigest;

internal class CommandLineOptions
{
    public bool ShowHelp       { get; private init; }
    public string? VideoUrl    { get; private init; }
    public string SubtitleLang { get; private init; } = "en";
    public string? SummaryLang { get; private init; }
    public int MaxVideos       { get; private init; } = Constants.MaxVideosPerChannel;

    public static CommandLineOptions Parse(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
            return new CommandLineOptions { ShowHelp = true };

        return new CommandLineOptions
        {
            VideoUrl    = args.FirstOrDefault(a => a.StartsWith("http://") || a.StartsWith("https://") || a.StartsWith("www.")),
            SubtitleLang = GetValue(args, "--lang") ?? "en",
            SummaryLang  = GetValue(args, "--summary-lang"),
            MaxVideos    = int.TryParse(GetValue(args, "--max-videos"), out var n) ? n : Constants.MaxVideosPerChannel
        };
    }

    private static string? GetValue(string[] args, string flag)
    {
        var index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
