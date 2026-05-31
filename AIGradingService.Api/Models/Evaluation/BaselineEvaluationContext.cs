using AIGradingService.Api.Models;

namespace AIGradingService.Api.Models.Evaluation;

public class BaselineEvaluationContext
{
    public Assignment Assignment { get; set; } = null!;
    public Submission Submission { get; set; } = null!;
    public TestRunResult TestRun { get; set; } = null!;
}