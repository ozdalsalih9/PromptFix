namespace PromptFix.Api.Models;

public sealed record PromptImproveResponse(
    string ImprovedPrompt,
    string ShortVersion,
    IReadOnlyList<string> WhyBetter,
    IReadOnlyList<string> MissingContext,
    string Model);
