using System.Text.Json.Serialization;

namespace PromptFix.Api.Models;

public sealed record OllamaTagsResponse(
    [property: JsonPropertyName("models")] IReadOnlyList<OllamaModelInfo>? Models);

public sealed record OllamaModelInfo(
    [property: JsonPropertyName("name")] string Name);
