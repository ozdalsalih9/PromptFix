using PromptFix.Api.Models;

namespace PromptFix.Api.Services;

public interface IOllamaService
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken);

    Task<bool> IsReachableAsync(CancellationToken cancellationToken);
}
