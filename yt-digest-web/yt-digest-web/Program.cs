using Microsoft.AspNetCore.Authentication.Cookies;
using YtDigestWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<YtDigestService>();

// If there is no cookie we get redirected to the login page.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
    });

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// REST API endpoint
app.MapPost("/api/summarize", async (SummarizeRequest request, YtDigestService ytDigest) =>
{
    if (string.IsNullOrWhiteSpace(request.Url))
        return Results.BadRequest("URL is required.");

    var summary = await ytDigest.SummarizeAsync(request.Url, request.Lang ?? "en");
    return Results.Ok(new { summary });
}).RequireAuthorization();

app.Run();

record SummarizeRequest(string Url, string? Lang);
