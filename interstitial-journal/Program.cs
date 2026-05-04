using InterstitialJournal;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});
var app = builder.Build();

//  Provide static files from wwwroot/ (index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

// Path to JSON file with entries
var dataFile = Path.Combine(app.Environment.ContentRootPath, "entries.json");


// --- API Endpoints ---

// GET /api/entries?date=2024-05-03
// Returns all entries, optionally filtered by date (yyyy-MM-dd)
app.MapGet("/api/entries", (string? date) =>
{
    var entries = Persistence.LoadEntries(dataFile);

    if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var filterDate))
        entries = entries.Where(e => DateOnly.FromDateTime(e.Timestamp) == filterDate).ToList();

    // Most recent entries first
    return entries.OrderByDescending(e => e.Timestamp).ToList();
});

// POST /api/entries
// Creates a new entry with the current timestamp
app.MapPost("/api/entries", (EntryInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Text))
        return Results.BadRequest("Text must not be empty.");

    var entries = Persistence.LoadEntries(dataFile);

    var newEntry = new Entry(
        Guid.NewGuid().ToString(),
        DateTime.Now,
        input.Text.Trim()
    );

    entries.Add(newEntry);
    Persistence.SaveEntries(dataFile, entries);

    return Results.Ok(newEntry);
});

// DELETE /api/entries/{id}
// Deletes a single entry
app.MapDelete("/api/entries/{id}", (string id) =>
{
    var entries = Persistence.LoadEntries(dataFile);
    var before = entries.Count;
    entries.RemoveAll(e => e.Id == id);

    if (entries.Count == before)
        return Results.NotFound();

    Persistence.SaveEntries(dataFile, entries);
    return Results.Ok();
});

app.Run();

// --- Data Models ---

// A stored entry
internal record Entry(string Id, DateTime Timestamp, string Text)
{
    public string DisplayTimestamp { get; init; } = Timestamp.ToString("HH:mm");
}

// The payload the browser sends when creating an entry
internal record EntryInput(string Text);
