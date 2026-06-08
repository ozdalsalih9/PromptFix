namespace PromptFix.Api.Services;

public interface IModelConcurrencyGate
{
    Task<IDisposable?> TryEnterAsync(CancellationToken cancellationToken);
}
