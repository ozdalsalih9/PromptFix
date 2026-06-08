namespace PromptFix.Api.Services;

public sealed class ModelBusyException : Exception
{
    public ModelBusyException(string message) : base(message)
    {
    }
}
