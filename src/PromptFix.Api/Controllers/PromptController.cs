using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PromptFix.Api.Models;
using PromptFix.Api.Services;

namespace PromptFix.Api.Controllers;

[ApiController]
[Route("api/prompt")]
[EnableRateLimiting(ApiConstants.RateLimitPolicy)]
public sealed class PromptController : ControllerBase
{
    private readonly IPromptOptimizerService _promptOptimizerService;

    public PromptController(IPromptOptimizerService promptOptimizerService)
    {
        _promptOptimizerService = promptOptimizerService;
    }

    [HttpPost("improve")]
    public async Task<IActionResult> Improve(PromptImproveRequest request, CancellationToken cancellationToken)
    {
        var errors = ValidationProblemFactory.Validate(request);
        if (errors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        try
        {
            var response = await _promptOptimizerService.ImproveAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ModelBusyException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
        }
        catch (OllamaUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }
}
