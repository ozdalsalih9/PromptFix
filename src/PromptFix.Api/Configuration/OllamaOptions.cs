namespace PromptFix.Api.Configuration;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "promptforge:4b";

    public int TimeoutSeconds { get; set; } = 180;

    public double Temperature { get; set; } = 0.3;

    public int NumContext { get; set; } = 2048;

    public int NumPredict { get; set; } = 600;

    public double TopP { get; set; } = 0.9;
}
