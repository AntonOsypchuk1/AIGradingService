namespace AIGradingService.Api.Models;

public class TestRunResult
{
    public string SubmissionId { get; set; } = string.Empty;
    public string AssignmentId { get; set; } = string.Empty;

    public int Passed { get; set; }
    public int Total { get; set; }

    public double PassedWeight { get; set; }
    public double TotalWeight { get; set; }

    public double WeightedPassRate => TotalWeight == 0
        ? 0
        : PassedWeight / TotalWeight;

    public List<TestCaseResult> Results { get; set; } = [];

    public bool HasRuntimeError => Results.Any(x => !string.IsNullOrWhiteSpace(x.Error));
}

public class TestCaseResult
{
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string ActualOutput { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public bool Passed { get; set; }

    public double Weight { get; set; } = 1;
}