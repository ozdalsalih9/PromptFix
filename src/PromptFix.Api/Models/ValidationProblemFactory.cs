namespace PromptFix.Api.Models;

public static class ValidationProblemFactory
{
    public static Dictionary<string, string[]> Validate(PromptImproveRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var prompt = request.Prompt?.Trim() ?? string.Empty;

        if (prompt.Length == 0)
        {
            errors["prompt"] = ["Prompt is required."];
        }
        else if (prompt.Length > PromptOptionCatalog.MaxPromptLength)
        {
            errors["prompt"] = [$"Prompt must be {PromptOptionCatalog.MaxPromptLength} characters or fewer."];
        }

        AddOptionError(errors, "mode", request.Mode, PromptOptionCatalog.Modes.Keys);
        AddOptionError(errors, "language", request.Language, PromptOptionCatalog.Languages.Keys);
        AddOptionError(errors, "style", request.Style, PromptOptionCatalog.Styles.Keys);

        return errors;
    }

    private static void AddOptionError(
        Dictionary<string, string[]> errors,
        string field,
        string? value,
        IEnumerable<string> allowedValues)
    {
        var allowed = allowedValues.ToArray();
        if (string.IsNullOrWhiteSpace(value) || !allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            errors[field] = [$"{field} must be one of: {string.Join(", ", allowed)}."];
        }
    }
}
