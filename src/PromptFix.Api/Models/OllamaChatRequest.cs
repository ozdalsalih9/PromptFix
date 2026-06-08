using System.Text.Json.Serialization;

namespace PromptFix.Api.Models;

public sealed record OllamaChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<OllamaMessage> Messages,
    [property: JsonPropertyName("stream")] bool Stream,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("options")] OllamaRequestOptions Options);

public sealed record OllamaMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

public sealed record OllamaRequestOptions(
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("num_ctx")] int NumContext,
    [property: JsonPropertyName("num_predict")] int NumPredict,
    [property: JsonPropertyName("top_p")] double TopP);
