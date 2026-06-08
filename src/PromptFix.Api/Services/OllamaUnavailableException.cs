namespace PromptFix.Api.Services;

public sealed class OllamaUnavailableException : Exception
{
    public OllamaUnavailableException(string message) : base(message)
    {
    }

    public OllamaUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
