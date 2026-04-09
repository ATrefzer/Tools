using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YtDigestWeb.Services;

namespace YtDigestWeb.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly YtDigestService _ytDigest;

    public IndexModel(YtDigestService ytDigest)
    {
        _ytDigest = ytDigest;
    }

    [BindProperty]
    public string VideoUrl { get; set; } = string.Empty;

    [BindProperty]
    public string Lang { get; set; } = "de";

    public string? Summary { get; set; }
    public string? Error { get; set; }

    public void OnGet() { }

    public async Task OnPostAsync()
    {
        try
        {
            Summary = await _ytDigest.SummarizeAsync(VideoUrl, Lang);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }
}
