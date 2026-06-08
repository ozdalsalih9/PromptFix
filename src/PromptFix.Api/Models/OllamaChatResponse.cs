using System.Text.Json.Serialization;

namespace PromptFix.Api.Models;

public sealed record OllamaChatResponse(
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("message")] OllamaMessage? Message,
    [property: JsonPropertyName("done")] bool Done);
