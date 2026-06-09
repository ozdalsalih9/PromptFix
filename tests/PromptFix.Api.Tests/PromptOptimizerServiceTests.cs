using Microsoft.Extensions.Options;
using PromptFix.Api.Configuration;
using PromptFix.Api.Models;
using PromptFix.Api.Services;

namespace PromptFix.Api.Tests;

public sealed class PromptOptimizerServiceTests
{
    [Fact]
    public async Task ImproveAsyncReturnsStableResponseFromGeneratedPrompt()
    {
        var service = CreateService(new FakeOllamaService("Act as a senior ATS resume writer..."));

        var response = await service.ImproveAsync(
            new PromptImproveRequest("bana cv hazirla", "career", "auto", "professional"),
            CancellationToken.None);

        Assert.Equal("Act as a senior ATS resume writer...", response.ImprovedPrompt);
        Assert.Equal("Act as a senior ATS resume writer...", response.ShortVersion);
        Assert.Equal(3, response.WhyBetter.Count);
        Assert.Contains(response.WhyBetter, item => item.Contains("career", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.WhyBetter, item => item.Contains("professional", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(response.MissingContext);
        Assert.Equal("qwen3.5:2b", response.Model);
    }

    [Fact]
    public async Task ImproveAsyncUsesPlainGeneratedText()
    {
        var service = CreateService(new FakeOllamaService("Here is an improved prompt without JSON."));

        var response = await service.ImproveAsync(
            new PromptImproveRequest("make better", "general", "en", "balanced"),
            CancellationToken.None);

        Assert.Equal("Here is an improved prompt without JSON.", response.ImprovedPrompt);
        Assert.Equal("Here is an improved prompt without JSON.", response.ShortVersion);
    }

    [Fact]
    public async Task ImproveAsyncCleansThinkingFallbackOutput()
    {
        var service = CreateService(new FakeOllamaService("""
            Thinking Process:
            analyze the request

            IMPROVED_PROMPT: Act as a senior resume writer and create a concise CV.
            """));

        var response = await service.ImproveAsync(
            new PromptImproveRequest("make cv", "career", "en", "balanced"),
            CancellationToken.None);

        Assert.Equal("Act as a senior resume writer and create a concise CV.", response.ImprovedPrompt);
    }

    [Fact]
    public async Task ImproveAsyncRemovesGeneratedOutputLabel()
    {
        var service = CreateService(new FakeOllamaService("Improved prompt: Act as a cat behavior specialist and explain safe next steps."));

        var response = await service.ImproveAsync(
            new PromptImproveRequest("kedi beni isirmaya calisiyor ne yapayim", "general", "tr", "balanced"),
            CancellationToken.None);

        Assert.Equal("Act as a cat behavior specialist and explain safe next steps.", response.ImprovedPrompt);
    }

    [Theory]
    [InlineData("general", "most relevant expert assistant")]
    [InlineData("coding", "senior software engineer")]
    [InlineData("career", "career coach")]
    [InlineData("academic", "academic writing or research assistant")]
    [InlineData("image", "image generation model")]
    [InlineData("email", "professional writing assistant")]
    public async Task ImproveAsyncAppliesModeSpecificRewriteRules(string mode, string expectedRule)
    {
        var ollama = new FakeOllamaService("Improved prompt");
        var service = CreateService(ollama);

        await service.ImproveAsync(
            new PromptImproveRequest("make this better", mode, "en", "balanced"),
            CancellationToken.None);

        Assert.Contains("MODE_RULES:", ollama.LastPrompt);
        Assert.Contains(expectedRule, ollama.LastPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("balanced", "Balance clarity")]
    [InlineData("concise", "1 or 2 sentences")]
    [InlineData("detailed", "4 to 7 sentences")]
    [InlineData("professional", "not a professional answer to the user")]
    public async Task ImproveAsyncAppliesStyleSpecificRewriteRules(string style, string expectedRule)
    {
        var ollama = new FakeOllamaService("Improved prompt");
        var service = CreateService(ollama);

        await service.ImproveAsync(
            new PromptImproveRequest("make this better", "general", "en", style),
            CancellationToken.None);

        Assert.Contains("STYLE_RULES:", ollama.LastPrompt);
        Assert.Contains(expectedRule, ollama.LastPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImproveAsyncRejectsSecondRequestWhenModelIsBusy()
    {
        var ollama = new FakeOllamaService("Improved prompt")
        {
            Delay = TimeSpan.FromMilliseconds(200)
        };

        var gate = new ModelConcurrencyGate();
        var first = CreateService(ollama, gate).ImproveAsync(
            new PromptImproveRequest("first", "general", "en", "balanced"),
            CancellationToken.None);

        await Assert.ThrowsAsync<ModelBusyException>(() => CreateService(ollama, gate).ImproveAsync(
            new PromptImproveRequest("second", "general", "en", "balanced"),
            CancellationToken.None));

        await first;
    }

    private static PromptOptimizerService CreateService(IOllamaService ollamaService, IModelConcurrencyGate? gate = null)
    {
        return new PromptOptimizerService(
            ollamaService,
            gate ?? new ModelConcurrencyGate(),
            Options.Create(new OllamaOptions { Model = "qwen3.5:2b" }));
    }

    private sealed class FakeOllamaService : IOllamaService
    {
        private readonly string _response;

        public FakeOllamaService(string response)
        {
            _response = response;
        }

        public TimeSpan Delay { get; init; } = TimeSpan.Zero;
        public string LastPrompt { get; private set; } = string.Empty;

        public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
        {
            LastPrompt = prompt;

            Assert.Contains("Never answer USER_DRAFT", prompt);
            Assert.Contains("Return only the improved prompt text", prompt);
            Assert.Contains("The selected mode and style are rewrite controls", prompt);

            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            return _response;
        }

        public Task<bool> IsReachableAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
