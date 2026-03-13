using System.Net.Http.Json;
using System.Text.Json;
using YoutubeDigest.Models;

namespace YoutubeDigest.Services;

/// <summary>
/// Communicates with the local Ollama REST API.
/// Ollama must be running: https://ollama.com
/// Load model with: ollama pull llama3.2
/// </summary>
public class OllamaService : ISummaryService
{
    public const string DefaultModel = "llama3.2";
    public const string DefaultUrl = "http://localhost:11434";

    private readonly HttpClient _http;
    private readonly string _model;
    private readonly Prompt _prompt;

    // Maximum character count of the transcript we send.
    // Ollama models have a context limit – at ~12,000 characters we're safe.
    private const int MaxTranscriptChars = 12_000;

    public OllamaService(Prompt prompt, string baseUrl = DefaultUrl, string model = DefaultModel)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };
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
            prompt = prompt,
            stream = false
        };

        var response = await _http.PostAsJsonAsync("/api/generate", requestBody);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var result = json.GetProperty("response").GetString()
                     ?? "Error: No response received from Ollama.";

        return result.TrimEnd() + BuildFooter(video.Id);
    }

    public string ModelName => _model;

    public string BuildFooter(string videoId)
    {
        return $"\n\n---\n*Summary created on {DateTime.Now:dd.MM.yyyy HH:mm}*\n" +
               $"*Video: https://www.youtube.com/watch?v={videoId}*\n";
    }
}
