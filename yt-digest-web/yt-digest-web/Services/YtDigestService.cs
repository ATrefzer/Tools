using System.Diagnostics;

namespace YtDigestWeb.Services;

public class YtDigestService
{
    /// <summary>
    /// Calls yt-digest as an external process and returns the markdown summary from stdout.
    /// yt-digest must be available in PATH.
    /// </summary>
    public async Task<string> SummarizeAsync(string videoUrl, string lang)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "yt-digest",
            Arguments = $"--lang {lang} \"{videoUrl}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("yt-digest could not be started.");

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"yt-digest failed: {error}");
        }

        return output.Trim();
    }
}
