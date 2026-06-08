using System.Text.Json.Serialization;

namespace PromptFix.Api.Models;

public sealed record OllamaGenerateResponse(
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("response")] string? Response,
    [property: JsonPropertyName("done")] bool Done);
