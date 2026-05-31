namespace AIGradingService.Api.Models.Evaluation;

public class EvaluationGrade
{
    public string BaselineName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;

    public string SubmissionId { get; set; } = string.Empty;
    public string AssignmentId { get; set; } = string.Empty;

    public int MaxScore { get; set; }
    public int ExpectedScore { get; set; }

    public double PredictedScore { get; set; }
    public double AbsoluteError { get; set; }

    public int PassedTests { get; set; }
    public int TotalTests { get; set; }

    public double PassedWeight { get; set; }
    public double TotalWeight { get; set; }
    public double PassRate { get; set; }

    public List<CriterionScore> Criteria { get; set; } = [];
    public List<string> RiskFlags { get; set; } = [];

    public string FinalDecisionPolicy { get; set; } = "teacher_must_confirm";
}