using YtDigestWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<YtDigestService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// REST API endpoint
app.MapPost("/api/summarize", async (SummarizeRequest request, YtDigestService ytDigest) =>
{
    if (string.IsNullOrWhiteSpace(request.Url))
        return Results.BadRequest("URL is required.");

    var summary = await ytDigest.SummarizeAsync(request.Url, request.Lang ?? "en");
    return Results.Ok(new { summary });
});

app.Run();

record SummarizeRequest(string Url, string? Lang);
