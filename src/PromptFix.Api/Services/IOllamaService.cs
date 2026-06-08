using PromptFix.Api.Models;

namespace PromptFix.Api.Services;

public interface IOllamaService
{
    Task<string> ChatAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken cancellationToken);

    Task<bool> IsReachableAsync(CancellationToken cancellationToken);
}
