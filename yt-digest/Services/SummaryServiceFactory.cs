namespace YoutubeDigest.Services;

/// <summary>
///     Factory to create the appropriate summary service based on configuration.
/// </summary>
public static class SummaryServiceFactory
{
    /// <summary>
    ///     Creates a summary service based on available configuration.
    ///     Priority:
    ///     1 claude.key file
    ///     2 ANTHROPIC_API_KEY env var
    ///     3 deepseek.key file
    ///     4 DEEPSEEK_API_KEY env var
    ///     5 Ollama fallback
    /// </summary>
    public static ISummaryService Create()
    {
        var prompt = new Prompt();

        var claudeKey = LoadKey(AppPaths.ClaudeKeyFile) ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(claudeKey))
            return new ClaudeService(claudeKey, prompt);

        var deepSeekKey = LoadKey(AppPaths.DeepSeekKeyFile) ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        if (!string.IsNullOrEmpty(deepSeekKey))
            return new DeepSeekService(deepSeekKey, prompt);

        return new OllamaService(prompt);
    }

    private static string? LoadKey(string path)
    {
        if (!File.Exists(path))
            return null;

        var key = File.ReadAllText(path).Trim();
        return string.IsNullOrEmpty(key) ? null : key;
    }
}