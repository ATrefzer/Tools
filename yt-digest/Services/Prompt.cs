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
                Analysiere das folgende Transkript und fasse die Kernaussagen zusammen.
                Antworte ausschließlich auf Deutsch. Den Video-Titel lasse bitte in seiner Originalsprache.

                Titel: {video.Title}

                Transkript:
                {transcript}

                Erstelle eine strukturierte Zusammenfassung in diesem Markdown-Format:

                ## {video.Title}

                ### Worum geht es?
                2–3 Sätze zur Kernaussage des Videos.

                ### Die wichtigsten Punkte
                Bullet-Liste der wichtigsten Aussagen mit Begründung.
                """;
    }
}