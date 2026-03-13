using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using YoutubeDigest.Models;

namespace YoutubeDigest.Services;

/// <summary>
/// Communicates with the Anthropic Claude API using the official SDK.
/// Requires ANTHROPIC_API_KEY environment variable.
/// https://console.anthropic.com/
/// </summary>
public class ClaudeService : ISummaryService
{
    public const string DefaultModel = "claude-sonnet-4-20250514";

    private readonly AnthropicClient _client;
    private readonly string _model;
    private readonly Prompt _prompt;

    // Claude has a large context window (200k tokens), so we can send more text.
    // ~100k characters is safe for most transcripts.
    private const int MaxTranscriptChars = 150_000;

    public ClaudeService(string apiKey, Prompt prompt, string model = DefaultModel)
    {
        _client = new AnthropicClient { ApiKey = apiKey };
        _model = model;
        _prompt = prompt;
    }

    public async Task<string> SummarizeAsync(VideoInfo video, string transcript)
    {
        if (transcript.Length > MaxTranscriptChars)
        {
            transcript = transcript[..MaxTranscriptChars] + "\n\n[Transcript was truncated]";
        }

        var prompt = _prompt.GetSummaryPrompt(video, transcript);

        var parameters = new MessageCreateParams
        {
            Model = _model,
            MaxTokens = 4096,
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = prompt
                }
            ]
        };

        var response = await _client.Messages.Create(parameters);

        // Extract text from the first content block's JSON
        var firstBlock = response.Content.FirstOrDefault();
        var content = "Error: No response received from Claude.";

        if (firstBlock?.Json != null)
        {
            // The JSON structure is: { "type": "text", "text": "..." }
            if (firstBlock.Json.TryGetProperty("text", out var textElement))
            {
                content = textElement.GetString() ?? content;
            }
        }

        return content.TrimEnd() + BuildFooter(video.Id);
    }

    public string ModelName => _model;

    private static string BuildFooter(string videoId)
    {
        return $"\n\n---\n*Summary created on {DateTime.Now:dd.MM.yyyy HH:mm}*\n" +
               $"*Video: https://www.youtube.com/watch?v={videoId}*\n";
    }
}
