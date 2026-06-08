using System.Text.Json;
using PromptFix.Api.Configuration;
using PromptFix.Api.Models;
using Microsoft.Extensions.Options;

namespace PromptFix.Api.Services;

public sealed class PromptOptimizerService : IPromptOptimizerService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        var rawOutput = await _ollamaService.ChatAsync(BuildMessages(request), cancellationToken);
        return ParseResponse(rawOutput);
    }

    private IReadOnlyList<OllamaMessage> BuildMessages(PromptImproveRequest request)
    {
        var mode = PromptOptionCatalog.Modes[request.Mode!];
        var language = PromptOptionCatalog.Languages[request.Language!];
        var style = PromptOptionCatalog.Styles[request.Style!];

        var systemPrompt = """
            You are PromptForge, a local prompt optimization engine.
            Your job is to rewrite weak, vague, messy, or incomplete user prompts into clear, structured, high-quality prompts.
            Do not answer the user's original request.
            Only improve the prompt.
            Preserve the user's intent.
            Preserve the user's language.
            If the input is Turkish, respond in Turkish.
            If the input is English, respond in English.
            Add useful structure: role, task, context, constraints, tone, and output format.
            Make the improved prompt copy-paste ready.
            If important information is missing, add a short missing context section.
            Return clean JSON only with these exact fields:
            improvedPrompt, shortVersion, whyBetter, missingContext.
            """;

        var userPrompt = $$"""
            Mode: {{mode}}
            Language: {{language}}
            Style: {{style}}

            Required JSON shape:
            {
              "improvedPrompt": "string",
              "shortVersion": "string",
              "whyBetter": ["string"],
              "missingContext": ["string"]
            }

            Original prompt:
            {{request.Prompt!.Trim()}}
            """;

        return
        [
            new OllamaMessage("system", systemPrompt),
            new OllamaMessage("user", userPrompt)
        ];
    }

    private PromptImproveResponse ParseResponse(string rawOutput)
    {
        var json = ExtractJsonObject(rawOutput);

        try
        {
            var parsed = JsonSerializer.Deserialize<OllamaPromptPayload>(json, JsonOptions);

            if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.ImprovedPrompt))
            {
                return new PromptImproveResponse(
                    parsed.ImprovedPrompt.Trim(),
                    string.IsNullOrWhiteSpace(parsed.ShortVersion) ? parsed.ImprovedPrompt.Trim() : parsed.ShortVersion.Trim(),
                    NormalizeList(parsed.WhyBetter),
                    NormalizeList(parsed.MissingContext),
                    _options.Model);
            }
        }
        catch (JsonException)
        {
            // Fall through to a safe response that still gives the user the model output.
        }

        return new PromptImproveResponse(
            rawOutput.Trim(),
            rawOutput.Trim(),
            ["The local model returned text that could not be parsed as the expected JSON shape."],
            [],
            _options.Model);
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray()
            ?? [];
    }

    private static string ExtractJsonObject(string value)
    {
        var start = value.IndexOf('{');
        var end = value.LastIndexOf('}');

        return start >= 0 && end > start
            ? value[start..(end + 1)]
            : value;
    }

    private sealed record OllamaPromptPayload(
        string? ImprovedPrompt,
        string? ShortVersion,
        IReadOnlyList<string>? WhyBetter,
        IReadOnlyList<string>? MissingContext);
}
