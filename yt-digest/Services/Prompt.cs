using YoutubeDigest.Models;

namespace YoutubeDigest.Services;

/// <summary>
///     Builds prompts for video summarization.
/// </summary>
public class Prompt
{
    public string GetSummaryPrompt(VideoInfo video, string transcript)
    {
        return $"""

                Analyze the following video transcript and summarize the key points.
                Keep the summary and video title in the language of the video transcript.

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