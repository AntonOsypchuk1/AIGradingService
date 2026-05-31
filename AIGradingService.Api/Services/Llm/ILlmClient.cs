namespace AIGradingService.Api.Services.Llm;

public interface ILlmClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}