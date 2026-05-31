namespace AIGradingService.Api.Models;

public class RuleBasedGrade
{
    public string SubmissionId { get; set; } = string.Empty;
    public string AssignmentId { get; set; } = string.Empty;

    public int MaxScore { get; set; }
    public int ExpectedScore { get; set; }

    public double PredictedScore { get; set; }
    public double AbsoluteError { get; set; }

    public int PassedTests { get; set; }
    public int TotalTests { get; set; }

    public List<CriterionScore> Criteria { get; set; } = [];
    public List<string> RiskFlags { get; set; } = [];
    public string FinalDecisionPolicy { get; set; } = "teacher_must_confirm";
}

public class CriterionScore
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public string Explanation { get; set; } = string.Empty;
}