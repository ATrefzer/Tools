using YoutubeDigest.Models;

namespace YoutubeDigest.Services;

/// <summary>
///     Builds prompts for video summarization.
/// </summary>
public class Prompt
{
    private readonly string? _summaryLanguage;

    public Prompt(string? summaryLanguage = null)
    {
        _summaryLanguage = summaryLanguage;
    }

    public string GetSummaryPrompt(VideoInfo video, string transcript)
    {
        var languageInstruction = _summaryLanguage is not null
            ? $"Generate the summary in {_summaryLanguage}."
            : "Keep the summary and video title in the language of the video transcript.";

        return $"""

                Analyze the following video transcript and summarize the key points.
                If there are any recommendations for action, make sure they are summarized in a clear and concise manner.
                {languageInstruction}

                Title: {video.Title}

                Transcript:
                {transcript}

                Create the structured summary in this Markdown format:

                ## {video.Title}

                ### What is it about?
                2–3 sentences about the key points of the video.

                ### The most important points
                Bullet list of the most important statements with justification.
                """;
    }
}
