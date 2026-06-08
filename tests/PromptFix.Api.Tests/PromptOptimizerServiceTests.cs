using Microsoft.Extensions.Options;
using PromptFix.Api.Configuration;
using PromptFix.Api.Models;
using PromptFix.Api.Services;

namespace PromptFix.Api.Tests;

public sealed class PromptOptimizerServiceTests
{
    [Fact]
    public async Task ImproveAsyncReturnsStableResponseFromOllamaJson()
    {
        var service = CreateService(new FakeOllamaService("""
            {
              "improvedPrompt": "Act as a senior ATS resume writer...",
              "shortVersion": "Create an ATS-friendly CV.",
              "whyBetter": ["Adds role and context.", "Defines output expectations."],
              "missingContext": ["Education", "Experience"]
            }
            """));

        var response = await service.ImproveAsync(
            new PromptImproveRequest("bana cv hazirla", "career", "auto", "professional"),
            CancellationToken.None);

        Assert.Equal("Act as a senior ATS resume writer...", response.ImprovedPrompt);
        Assert.Equal("Create an ATS-friendly CV.", response.ShortVersion);
        Assert.Equal(2, response.WhyBetter.Count);
        Assert.Equal(2, response.MissingContext.Count);
        Assert.Equal("promptforge:2b", response.Model);
    }

    [Fact]
    public async Task ImproveAsyncReturnsFallbackWhenModelOutputIsNotJson()
    {
        var service = CreateService(new FakeOllamaService("Here is an improved prompt without JSON."));

        var response = await service.ImproveAsync(
            new PromptImproveRequest("make better", "general", "en", "balanced"),
            CancellationToken.None);

        Assert.Equal("Here is an improved prompt without JSON.", response.ImprovedPrompt);
        Assert.Contains("could not be parsed", response.WhyBetter[0]);
    }

    [Fact]
    public async Task ImproveAsyncRejectsSecondRequestWhenModelIsBusy()
    {
        var ollama = new FakeOllamaService("""
            {
              "improvedPrompt": "Improved",
              "shortVersion": "Short",
              "whyBetter": ["Clearer"],
              "missingContext": []
            }
            """)
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
            Options.Create(new OllamaOptions { Model = "promptforge:2b" }));
    }

    private sealed class FakeOllamaService : IOllamaService
    {
        private readonly string _response;

        public FakeOllamaService(string response)
        {
            _response = response;
        }

        public TimeSpan Delay { get; init; } = TimeSpan.Zero;

        public async Task<string> ChatAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken cancellationToken)
        {
            Assert.Contains(messages, message => message.Role == "system" && message.Content.Contains("Do not answer"));
            Assert.Contains(messages, message => message.Role == "user" && message.Content.Contains("Return JSON only"));

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
