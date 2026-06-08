using PromptFix.Api.Models;

namespace PromptFix.Api.Tests;

public sealed class PromptValidationTests
{
    [Fact]
    public void ValidateRejectsEmptyPrompt()
    {
        var result = ValidationProblemFactory.Validate(new PromptImproveRequest("", "general", "auto", "balanced"));

        Assert.Contains("prompt", result.Keys);
    }

    [Fact]
    public void ValidateRejectsInvalidOptions()
    {
        var result = ValidationProblemFactory.Validate(new PromptImproveRequest("Improve this", "sales", "de", "casual"));

        Assert.Contains("mode", result.Keys);
        Assert.Contains("language", result.Keys);
        Assert.Contains("style", result.Keys);
    }

    [Fact]
    public void ValidateAcceptsValidRequest()
    {
        var result = ValidationProblemFactory.Validate(new PromptImproveRequest("Improve this", "coding", "en", "detailed"));

        Assert.Empty(result);
    }
}
