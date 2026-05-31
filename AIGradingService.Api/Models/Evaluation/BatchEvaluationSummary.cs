namespace AIGradingService.Api.Models.Evaluation;

public class BatchEvaluationSummary
{
    public string BaselineName { get; set; } = string.Empty;

    public int TotalSubmissions { get; set; }
    public double AverageAbsoluteError { get; set; }
    public double ExactMatchAccuracy { get; set; }
    public double WithinOneAccuracy { get; set; }

    public List<EvaluationGrade> Grades { get; set; } = [];
}