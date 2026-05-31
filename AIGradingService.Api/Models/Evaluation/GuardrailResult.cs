namespace AIGradingService.Api.Models.Evaluation;

public class GuardrailResult
{
    public double OriginalScore { get; set; }
    public double FinalScore { get; set; }
    public double? AppliedCap { get; set; }
    public List<string> AppliedRules { get; set; } = [];
}