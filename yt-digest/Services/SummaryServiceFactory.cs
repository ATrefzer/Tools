namespace YoutubeDigest.Services;

/// <summary>
///     Factory to create the appropriate summary service based on configuration.
/// </summary>
public static class SummaryServiceFactory
{
    private static readonly string KeyFile = AppPaths.KeyFile;

    /// <summary>
    ///     Creates a summary service based on available configuration.
    ///     Priority: 1) key.txt file, 2) ANTHROPIC_API_KEY env var, 3) Ollama fallback
    /// </summary>
    public static ISummaryService Create()
    {
        var promptBuilder = new Prompt();
        var anthropicApiKey = LoadAnthropicApiKey();

        if (!string.IsNullOrEmpty(anthropicApiKey))
        {
            var service = new ClaudeService(anthropicApiKey, promptBuilder);
            return service;
        }
        else
        {
            var service = new OllamaService(promptBuilder);
            return service;
        }
    }

    private static string? LoadAnthropicApiKey()
    {
        // 1. Try key.txt file first
        if (File.Exists(KeyFile))
        {
            var key = File.ReadAllText(KeyFile).Trim();
            if (!string.IsNullOrEmpty(key))
            {
                return key;
            }
        }

        // 2. Fall back to environment variable
        return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }
}