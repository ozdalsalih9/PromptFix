namespace PromptFix.Api.Models;

public static class PromptOptionCatalog
{
    public const int MaxPromptLength = 4_000;

    public static readonly IReadOnlyDictionary<string, string> Modes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["general"] = "general prompt improvement",
        ["coding"] = "software development, debugging, architecture, and code generation",
        ["career"] = "CV, resume, interview, job application, and career communication",
        ["academic"] = "academic writing, research, study, and citation-oriented tasks",
        ["image"] = "image generation prompts with visual details and constraints",
        ["email"] = "clear, professional email and message writing"
    };

    public static readonly IReadOnlyDictionary<string, string> Languages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["auto"] = "preserve the user's detected language",
        ["tr"] = "Turkish",
        ["en"] = "English"
    };

    public static readonly IReadOnlyDictionary<string, string> Styles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["balanced"] = "balanced and practical",
        ["concise"] = "concise and direct",
        ["detailed"] = "detailed and explicit",
        ["professional"] = "professional and polished"
    };
}
