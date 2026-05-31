using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AIGradingService.Api.Services.Llm;

public class OpenRouterLlmClient : ILlmClient
{
    public string ModelName => _options.Model;
    
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterLlmClient(
        HttpClient httpClient,
        IOptions<OpenRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> CompleteAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenRouter API key is missing.");

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        request.Headers.Add("HTTP-Referer", _options.HttpReferer);
        request.Headers.Add("X-OpenRouter-Title", _options.AppTitle);

        var body = new
        {
            model = _options.Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.0
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenRouter request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseContent}");
        }

        using var json = JsonDocument.Parse(responseContent);

        var content = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenRouter returned empty content.");

        return ExtractJson(content);
    }

    private static string ExtractJson(string content)
    {
        content = content.Trim();

        if (content.StartsWith("```"))
        {
            var firstBrace = content.IndexOf('{');
            var lastBrace = content.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
                return content[firstBrace..(lastBrace + 1)];
        }

        return content;
    }
}