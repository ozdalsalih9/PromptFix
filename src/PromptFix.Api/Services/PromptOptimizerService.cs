using PromptFix.Api.Configuration;
using PromptFix.Api.Models;
using Microsoft.Extensions.Options;

namespace PromptFix.Api.Services;

public sealed class PromptOptimizerService : IPromptOptimizerService
{
    private readonly IOllamaService _ollamaService;
    private readonly IModelConcurrencyGate _concurrencyGate;
    private readonly OllamaOptions _options;

    public PromptOptimizerService(
        IOllamaService ollamaService,
        IModelConcurrencyGate concurrencyGate,
        IOptions<OllamaOptions> options)
    {
        _ollamaService = ollamaService;
        _concurrencyGate = concurrencyGate;
        _options = options.Value;
    }

    public async Task<PromptImproveResponse> ImproveAsync(PromptImproveRequest request, CancellationToken cancellationToken)
    {
        using var gate = await _concurrencyGate.TryEnterAsync(cancellationToken);
        if (gate is null)
        {
            throw new ModelBusyException("The local model is busy. Please try again in a few seconds.");
        }

        var improvedPrompt = await _ollamaService.GenerateAsync(BuildPrompt(request), cancellationToken);
        return BuildResponse(improvedPrompt);
    }

    private string BuildPrompt(PromptImproveRequest request)
    {
        var mode = PromptOptionCatalog.Modes[request.Mode!];
        var language = PromptOptionCatalog.Languages[request.Language!];
        var style = PromptOptionCatalog.Styles[request.Style!];

        return $"Rewrite this weak prompt into a better prompt. Do not answer the request. Do not explain. Return only the improved prompt. Mode: {mode}. Language: {language}. Style: {style}. Prompt: {request.Prompt!.Trim()}";
    }

    private PromptImproveResponse BuildResponse(string improvedPrompt)
    {
        var normalized = CleanModelOutput(improvedPrompt);

        return new PromptImproveResponse(
            normalized,
            normalized,
            ["Rewrites the request as a clearer, copy-paste-ready prompt."],
            [],
            _options.Model);
    }

    private static string CleanModelOutput(string value)
    {
        var normalized = value.Trim();
        var promptMarker = normalized.LastIndexOf("Prompt:", StringComparison.OrdinalIgnoreCase);

        if (normalized.Contains("Thinking Process", StringComparison.OrdinalIgnoreCase) && promptMarker >= 0)
        {
            return normalized[promptMarker..].Trim();
        }

        return normalized;
    }
}
