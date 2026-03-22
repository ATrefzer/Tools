using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using YoutubeDigest.Models;

namespace YoutubeDigest.Services;

/// <summary>
/// Communicates with the DeepSeek API using the OpenAI-compatible chat completions endpoint.
/// Requires a DeepSeek API key: https://platform.deepseek.com/
/// </summary>
public class DeepSeekService : ISummaryService
{
    public const string DefaultModel = "deepseek-chat";
    private const string ApiUrl = "https://api.deepseek.com/chat/completions";

    private readonly HttpClient _http;
    private readonly string _model;
    private readonly Prompt _prompt;

    // DeepSeek V3 has a 64k token context window; ~60k chars is safe.
    private const int MaxTranscriptChars = 60_000;

    public DeepSeekService(string apiKey, Prompt prompt, string model = DefaultModel)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
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

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            stream = false
        };

        var response = await _http.PostAsJsonAsync(ApiUrl, requestBody);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var result = json
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?? "Error: No response received from DeepSeek.";

        return result.TrimEnd() + BuildFooter(video.Id);
    }

    public string ModelName => _model;

    private static string BuildFooter(string videoId)
    {
        return $"\n\n---\n*Summary created on {DateTime.Now:dd.MM.yyyy HH:mm}*\n" +
               $"*Video: https://www.youtube.com/watch?v={videoId}*\n";
    }
}
