using PromptFix.Api.Models;

namespace PromptFix.Api.Services;

public interface IPromptOptimizerService
{
    Task<PromptImproveResponse> ImproveAsync(PromptImproveRequest request, CancellationToken cancellationToken);
}
