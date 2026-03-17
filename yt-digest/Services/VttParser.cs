using System.Text;
using System.Text.RegularExpressions;

namespace YoutubeDigest.Services
{
    public record VttCue(string Start, string End, string Text);

    public static class VttParser
    {
        /// <summary>
        ///     Parses a .vtt file into readable plain text.
        /// </summary>
        public static string ParseFile(string vttFile)
        {
            // Normalize line endings to \n for consistent parsing
            var vttContent = File.ReadAllText(vttFile).Replace("\r\n", "\n");

            var text = ParseKaraokeFormat(vttContent);
            if (string.IsNullOrEmpty(text))
            {
                text = ParseSimpleFormat(vttContent);
            }

            return text;
        }


        private static string ParseSimpleFormat(string vttContent)
        {
            var result = new StringBuilder();
            var lastLine = "";

            var lines = vttContent.Split('\n');
            var nextLinesAreText = false;

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (line.Contains("-->"))
                {
                    nextLinesAreText = true;
                    continue;
                }

                if (!nextLinesAreText)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    nextLinesAreText = false;
                    continue;
                }

                // Skip Cue-ID or metadata
                if (line.StartsWith("NOTE") || line.StartsWith("WEBVTT") || line.StartsWith("Kind:"))
                {
                    nextLinesAreText = false;
                    continue;
                }

                var cleaned = CleanHtmlTags(line);

                if (string.IsNullOrEmpty(cleaned) || cleaned == lastLine) continue;

                result.Append(cleaned).Append(' ');
                lastLine = cleaned;
            }

            return result.ToString().Trim();
        }


        /// <summary>
        /// Cue blocks are separated by " \n" (space + Newline) in karaoke-style .vtt files.
        /// This method extracts the last non-empty line of each block, which usually contains the main text,
        /// while skipping timestamps and metadata.
        /// </summary>
        private static string ParseKaraokeFormat(string vttContent)
        {
            var result = new StringBuilder();
            var lastLine = "";

            // Cue blocks are separated by " \n" (space + Newline)
            var blocks = vttContent.Split(" \n");

            if (blocks.Length == 1)
            {
                // No cue separator found, try other strategy.
                return string.Empty;
            }

            foreach (var block in blocks)
            {
                // Last non empty line
                var lastTextLine = block
                    .Split('\n')
                    .LastOrDefault(l => !string.IsNullOrWhiteSpace(l));

                if (lastTextLine is null) continue;

                // Skip headers and timestamps (should be only the first block)
                if (lastTextLine.Contains("WEBVTT") ||
                    lastTextLine.Contains("Kind:") ||
                    lastTextLine.Contains("NOTE") ||
                    lastTextLine.Contains("-->"))
                    continue;


                var cleaned = CleanHtmlTags(lastTextLine);

                // Skip empty lines and duplicates
                if (string.IsNullOrEmpty(cleaned) || cleaned == lastLine) continue;

                result.Append(cleaned).Append(' ');
                lastLine = cleaned;
            }

            return result.ToString().Trim();
        }

        private static string CleanHtmlTags(string input)
        {
            // Remove inline tags: <c>, </c>, <00:00:12.000>, etc.
            return Regex.Replace(input, "<[^>]+>", "").Trim();
        }
    }
}