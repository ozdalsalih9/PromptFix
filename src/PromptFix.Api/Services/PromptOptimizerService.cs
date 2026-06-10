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
        return BuildResponse(improvedPrompt, request);
    }

    private string BuildPrompt(PromptImproveRequest request)
    {
        var mode = PromptOptionCatalog.Modes[request.Mode!];
        var language = PromptOptionCatalog.Languages[request.Language!];
        var style = PromptOptionCatalog.Styles[request.Style!];
        var modeRules = GetModeRules(request.Mode!);
        var styleRules = GetStyleRules(request.Style!);

        return $"""
            You are PromptForge. Rewrite USER_DRAFT into a better prompt for another AI assistant.
            Do not answer USER_DRAFT. Do not give advice, facts, warnings, solutions, markdown, labels, JSON, or quotes.
            Return only the improved prompt text. It must read like an instruction to another AI assistant.

            Language: {language}
            Mode: {mode}
            Style: {style}

            Global rewrite rules:
            - Preserve the user's intent.
            - Add a useful expert role when helpful.
            - Ask for a practical, structured answer and include missing context requests.
            - If the draft asks what to do, rewrite it as a prompt asking another AI to explain what to do.
            - Keep the improved prompt concise enough for quick inference.

            MODE_RULES:
            {modeRules}

            STYLE_RULES:
            {styleRules}

            USER_DRAFT: {request.Prompt!.Trim()}
            IMPROVED_PROMPT:
            """;
    }

    private PromptImproveResponse BuildResponse(string improvedPrompt, PromptImproveRequest request)
    {
        var normalized = CleanModelOutput(improvedPrompt);
        var modeLabel = PromptOptionCatalog.Modes[request.Mode!];
        var styleLabel = PromptOptionCatalog.Styles[request.Style!];

        return new PromptImproveResponse(
            normalized,
            normalized,
            [
                "Rewrites the request as a copy-paste-ready prompt instead of answering it.",
                $"Applies mode-specific structure for {modeLabel}.",
                $"Uses a {styleLabel} prompt style."
            ],
            [],
            _options.Model);
    }

    private static string GetModeRules(string mode)
    {
        return mode.ToLowerInvariant() switch
        {
            "coding" => """
                - Make the prompt address a senior software engineer.
                - Include the programming goal, relevant stack, current behavior, expected behavior, constraints, and acceptance criteria.
                - Ask for code, debugging steps, architecture guidance, tests, or tradeoffs only when they match the draft.
                """,
            "career" => """
                - Make the prompt address a career coach, recruiter, resume writer, or interview coach.
                - Include target role, seniority, experience, achievements, skills, tone, and output format.
                - Ask for missing career details before producing final material when the draft lacks them.
                """,
            "academic" => """
                - Make the prompt address an academic writing or research assistant.
                - Include topic, thesis or research question, scope, method, citation expectations, structure, and academic tone.
                - Ask for missing source, level, field, and formatting requirements when needed.
                """,
            "image" => """
                - Make the prompt suitable for an image generation model.
                - Include subject, setting, composition, visual style, lighting, color palette, camera/framing, mood, and constraints.
                - If the draft refers to an existing image, write an image-editing prompt and ask the assistant to improve the provided image.
                """,
            "email" => """
                - Make the prompt address a professional writing assistant.
                - Include recipient, relationship, goal, key points, desired tone, subject line, call to action, and length.
                - Ask for missing context that would change the message.
                """,
            _ => """
                - Make the prompt address the most relevant expert assistant for the draft.
                - Include goal, context, constraints, desired format, audience, and missing information.
                - Keep the result broadly useful without forcing a specialized domain.
                """
        };
    }

    private static string GetStyleRules(string style)
    {
        return style.ToLowerInvariant() switch
        {
            "concise" => """
                - Write a compact prompt in 1 or 2 sentences.
                - Keep only the essential role, task, constraints, and output expectation.
                - Avoid long lists unless the draft absolutely requires them.
                """,
            "detailed" => """
                - Write a fuller prompt with explicit context, objectives, constraints, output format, and quality criteria.
                - Include useful sub-questions or missing context requests inside the prompt.
                - Prefer 4 to 7 sentences when the draft is short.
                """,
            "professional" => """
                - Write a polished, formal prompt suitable for business or expert use.
                - Keep the output as an instruction to another AI assistant, not a professional answer to the user.
                - Include deliverables, tone, constraints, and a clean structure request.
                """,
            _ => """
                - Write a practical prompt with enough detail to improve the result without becoming verbose.
                - Balance clarity, context, constraints, and output format.
                - Prefer 2 to 4 sentences when the draft is short.
                """
        };
    }

    private static string CleanModelOutput(string value)
    {
        var normalized = value.Trim();
        var promptMarker = normalized.LastIndexOf("IMPROVED_PROMPT:", StringComparison.OrdinalIgnoreCase);

        if (normalized.Contains("Thinking Process", StringComparison.OrdinalIgnoreCase) && promptMarker >= 0)
        {
            normalized = normalized[(promptMarker + "IMPROVED_PROMPT:".Length)..].Trim();
        }

        var removablePrefixes = new[]
        {
            "IMPROVED_PROMPT:",
            "Improved prompt:",
            "Prompt:",
            "Output:",
            "Cikti:",
            "Çıktı:"
        };

        foreach (var prefix in removablePrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[prefix.Length..].Trim();
                break;
            }
        }

        return normalized.Trim().Trim('"', '\'');
    }
}
