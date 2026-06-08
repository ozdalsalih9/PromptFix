using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PromptFix.Api.Configuration;
using PromptFix.Api.Services;

namespace PromptFix.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IOllamaService _ollamaService;
    private readonly OllamaOptions _options;

    public HealthController(IOllamaService ollamaService, IOptions<OllamaOptions> options)
    {
        _ollamaService = ollamaService;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var ollamaReachable = await _ollamaService.IsReachableAsync(cancellationToken);

        return Ok(new
        {
            backend = "ok",
            ollamaReachable,
            ollamaBaseUrl = _options.BaseUrl,
            model = _options.Model
        });
    }
}
