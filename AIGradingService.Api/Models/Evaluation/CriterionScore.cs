namespace AIGradingService.Api.Models.Evaluation;

public class CriterionScore
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public string Explanation { get; set; } = string.Empty;
}