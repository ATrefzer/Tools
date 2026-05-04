using System.Text.Json;
using System.Text.Json.Serialization;

namespace InterstitialJournal;

internal class Persistence
{
    public static List<Entry> LoadEntries(string dataFile)
    {
        if (!File.Exists(dataFile)) return new List<Entry>();
        var json = File.ReadAllText(dataFile);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.ListEntry) ?? new List<Entry>();
    }

    public static void SaveEntries(string dataFile, List<Entry> entries)
    {
        var json = JsonSerializer.Serialize(entries, AppJsonContext.Default.ListEntry);
        File.WriteAllText(dataFile, json);
    }
}

// Source-generated JSON serialization context for AOT/self-contained compatibility
[JsonSerializable(typeof(List<Entry>))]
[JsonSerializable(typeof(Entry))]
[JsonSerializable(typeof(EntryInput))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AppJsonContext : JsonSerializerContext;