using System.Text.Json.Serialization;

namespace PromptFix.Api.Models;

public sealed record OllamaGenerateRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("stream")] bool Stream,
    [property: JsonPropertyName("think")] bool Think,
    [property: JsonPropertyName("keep_alive")] string KeepAlive,
    [property: JsonPropertyName("options")] OllamaRequestOptions Options);

public sealed record OllamaRequestOptions(
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("num_ctx")] int NumContext,
    [property: JsonPropertyName("num_predict")] int NumPredict,
    [property: JsonPropertyName("top_p")] double TopP);
