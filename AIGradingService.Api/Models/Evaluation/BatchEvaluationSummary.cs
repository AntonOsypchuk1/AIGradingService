namespace AIGradingService.Api.Models.Evaluation;

public class BatchEvaluationSummary
{
    public string BaselineName { get; set; } = string.Empty;

    public int TotalSubmissions { get; set; }
    public int FailedEvaluations { get; set; }
    public double SuccessRate { get; set; }

    public double AverageAbsoluteError { get; set; }
    public double ExactMatchAccuracy { get; set; }
    public double WithinOneAccuracy { get; set; }

    public double MaxAbsoluteError { get; set; }
    public double LargeErrorRate { get; set; }

    public double OverestimationRate { get; set; }
    public double UnderestimationRate { get; set; }

    public int OverestimatedCount { get; set; }
    public int UnderestimatedCount { get; set; }
    public int ExactMatchCount { get; set; }
    public int WithinOneCount { get; set; }
    public int LargeErrorCount { get; set; }

    public double AveragePredictedScore { get; set; }
    public double AverageExpectedScore { get; set; }

    public int GuardrailAppliedCount { get; set; }
    public double GuardrailAppliedRate { get; set; }

    public List<EvaluationGrade> Grades { get; set; } = [];
}