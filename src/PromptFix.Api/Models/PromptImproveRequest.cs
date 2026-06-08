namespace PromptFix.Api.Models;

public sealed record PromptImproveRequest(
    string? Prompt,
    string? Mode,
    string? Language,
    string? Style);
