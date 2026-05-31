namespace AIGradingService.Api.Models.Evaluation;

public class LlmGradeResult
{
    public double PredictedScore { get; set; }
    public List<CriterionScore> Criteria { get; set; } = [];
    public List<string> RiskFlags { get; set; } = [];
    public string Explanation { get; set; } = string.Empty;
}