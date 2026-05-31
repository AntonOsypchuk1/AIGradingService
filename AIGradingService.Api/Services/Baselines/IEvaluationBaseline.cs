using AIGradingService.Api.Models.Evaluation;

namespace AIGradingService.Api.Services.Baselines;

public interface IEvaluationBaseline
{
    string Name { get; }

    Task<EvaluationGrade> EvaluateAsync(
        BaselineEvaluationContext context,
        CancellationToken cancellationToken = default);
}