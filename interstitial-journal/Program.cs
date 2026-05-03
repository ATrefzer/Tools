using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//  Provide static files from wwwroot/ (index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

// Path to JSON file with entries
var dataFile = Path.Combine(app.Environment.ContentRootPath, "entries.json");

// --- Helper functions for reading and writing ---

List<Entry> LoadEntries()
{
    if (!File.Exists(dataFile)) return new List<Entry>();
    var json = File.ReadAllText(dataFile);
    return JsonSerializer.Deserialize<List<Entry>>(json) ?? new List<Entry>();
}

void SaveEntries(List<Entry> entries)
{
    var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(dataFile, json);
}

// --- API Endpoints ---

// GET /api/entries?date=2024-05-03
// Returns all entries, optionally filtered by date (yyyy-MM-dd)
app.MapGet("/api/entries", (string? date) =>
{
    var entries = LoadEntries();

    if (!string.IsNullOrEmpty(date))
    {
        entries = entries.Where(e => e.Timestamp.StartsWith(date)).ToList();
    }

    // Most recent entries first
    return entries.OrderByDescending(e => e.Timestamp).ToList();
});

// POST /api/entries
// Creates a new entry with the current timestamp
app.MapPost("/api/entries", (EntryInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Text))
        return Results.BadRequest("Text must not be empty.");

    var entries = LoadEntries();

    var newEntry = new Entry(
        Id: Guid.NewGuid().ToString(),
        Timestamp: DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        Text: input.Text.Trim()
    );

    entries.Add(newEntry);
    SaveEntries(entries);

    return Results.Ok(newEntry);
});

// DELETE /api/entries/{id}
// Deletes a single entry
app.MapDelete("/api/entries/{id}", (string id) =>
{
    var entries = LoadEntries();
    var before = entries.Count;
    entries.RemoveAll(e => e.Id == id);

    if (entries.Count == before)
        return Results.NotFound();

    SaveEntries(entries);
    return Results.Ok();
});

app.Run();

// --- Data Models ---

// A stored entry
record Entry(string Id, string Timestamp, string Text);

// The payload the browser sends when creating an entry
record EntryInput(string Text);
