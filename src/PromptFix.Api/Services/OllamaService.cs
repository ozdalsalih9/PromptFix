using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PromptFix.Api.Configuration;
using PromptFix.Api.Models;

namespace PromptFix.Api.Services;

public sealed class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaService(HttpClient httpClient, IOptions<OllamaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> ChatAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken cancellationToken)
    {
        var request = new OllamaChatRequest(
            _options.Model,
            messages,
            false,
            "json",
            new OllamaRequestOptions(
                _options.Temperature,
                _options.NumContext,
                _options.NumPredict,
                _options.TopP));

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
            var content = ollamaResponse?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new OllamaUnavailableException("Ollama returned an empty response.");
            }

            return content;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OllamaUnavailableException("The local model took too long to respond. Try a shorter prompt or try again after the model warms up.");
        }
        catch (HttpRequestException ex)
        {
            throw new OllamaUnavailableException("Ollama is unavailable. Confirm it is running on localhost:11434.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OllamaUnavailableException("The local model took too long to respond. Try a shorter prompt or try again after the model warms up.", ex);
        }
    }

    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
