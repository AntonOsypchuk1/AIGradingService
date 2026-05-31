namespace AIGradingService.Api.Models;

public class BatchEvaluationResult
{
    public int TotalSubmissions { get; set; }
    public double AverageAbsoluteError { get; set; }
    public double ExactMatchAccuracy { get; set; }
    public double WithinOneAccuracy { get; set; }
    public List<RuleBasedGrade> Grades { get; set; } = [];
}