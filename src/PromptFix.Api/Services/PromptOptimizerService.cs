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

        return $"""
            You are PromptForge, a prompt rewriting engine.

            Your only job is to transform USER_DRAFT into a stronger prompt for another AI assistant.
            Never answer USER_DRAFT.
            Never give advice, facts, solutions, warnings, or explanations about USER_DRAFT.
            Never write as if you are responding to the user directly.
            Return only the improved prompt text. Do not use markdown, labels, JSON, or quotes.

            The improved prompt must:
            - Preserve the user's original intent.
            - Preserve the selected language: {language}.
            - Match the selected mode: {mode}.
            - Match the selected style: {style}.
            - Start with a useful expert role when appropriate.
            - Ask the future AI assistant for a practical, structured answer.
            - Add missing context requirements as questions or assumptions inside the prompt.

            Examples:
            USER_DRAFT: kedi beni isirmaya calisiyor ne yapayim
            IMPROVED_PROMPT: Bir kedi davranisi uzmani gibi davran. Kedimin beni isirmaya calismasinin olasi nedenlerini acikla, acil risk isaretlerini belirt ve guvenli sekilde ne yapmam gerektigini adim adim anlat. Cevabinda kedinin yasi, kisir olup olmadigi, isirma davranisinin ne zaman basladigi, oyun mu saldirganlik mi oldugu ve ortamda stres kaynagi bulunup bulunmadigi gibi eksik bilgileri de sor.

            USER_DRAFT: bana cv hazirla
            IMPROVED_PROMPT: Bir ATS odakli kariyer danismani gibi davran. Bana profesyonel ve net bir CV taslagi hazirlamak icin once hedef pozisyonumu, deneyimlerimi, egitimimi, teknik becerilerimi, basarilarimi ve tercih ettigim dili sor. Ardindan bu bilgilere gore baslik, ozet, deneyim, egitim, beceriler ve projeler bolumlerinden olusan duzenli bir CV metni uret.

            USER_DRAFT:
            {request.Prompt!.Trim()}

            IMPROVED_PROMPT:
            """;
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
