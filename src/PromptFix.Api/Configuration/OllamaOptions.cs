namespace PromptFix.Api.Configuration;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "qwen3.5:2b";

    public int TimeoutSeconds { get; set; } = 90;

    public double Temperature { get; set; } = 0.3;

    public int NumContext { get; set; } = 1024;

    public int NumPredict { get; set; } = 180;

    public double TopP { get; set; } = 0.9;

    public string KeepAlive { get; set; } = "10m";
}
