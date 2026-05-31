namespace AIGradingService.Api.Services.Llm;

public class OpenRouterOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
    public string AppTitle { get; set; } = "AIGradingService";
    public string HttpReferer { get; set; } = "http://localhost";
}