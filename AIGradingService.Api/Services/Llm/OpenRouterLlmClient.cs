using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AIGradingService.Api.Services.Llm;

public class OpenRouterLlmClient : ILlmClient
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(60);

    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterLlmClient> _logger;

    public OpenRouterLlmClient(
        HttpClient httpClient,
        IOptions<OpenRouterOptions> options,
        ILogger<OpenRouterLlmClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string ModelName => _options.Model;

    public async Task<string> CompleteAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenRouter API key is missing.");

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var request = CreateRequest(prompt);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return ParseOpenRouterContent(responseContent);
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == MaxAttempts)
                {
                    throw new InvalidOperationException(
                        $"OpenRouter request failed after {MaxAttempts} attempts with 429 Too Many Requests. Body: {responseContent}");
                }

                var delay = GetRetryDelay(response);

                _logger.LogWarning(
                    "OpenRouter returned 429. Attempt {Attempt}/{MaxAttempts}. Waiting {DelaySeconds} seconds before retry.",
                    attempt,
                    MaxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
                continue;
            }

            throw new InvalidOperationException(
                $"OpenRouter request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseContent}");
        }

        throw new InvalidOperationException("OpenRouter request failed unexpectedly.");
    }

    private HttpRequestMessage CreateRequest(string prompt)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);

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

        return request;
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is not null)
            return retryAfter.Delta.Value;

        if (retryAfter?.Date is not null)
        {
            var delay = retryAfter.Date.Value - DateTimeOffset.UtcNow;

            if (delay > TimeSpan.Zero)
                return delay;
        }

        return DefaultRetryDelay;
    }

    private static string ParseOpenRouterContent(string responseContent)
    {
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

        var firstBrace = content.IndexOf('{');
        var lastBrace = content.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
            return content[firstBrace..(lastBrace + 1)];

        return content;
    }
}