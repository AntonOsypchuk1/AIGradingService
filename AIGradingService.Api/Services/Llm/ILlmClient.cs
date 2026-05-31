namespace AIGradingService.Api.Services.Llm;

public interface ILlmClient
{
    string ModelName { get; }
    
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}