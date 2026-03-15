using System.Text.Json.Serialization;
using YoutubeDigest.Models;

namespace YoutubeDigest;

[JsonSerializable(typeof(Dictionary<string, ProcessedVideo>))]
internal partial class AppJsonContext : JsonSerializerContext;
