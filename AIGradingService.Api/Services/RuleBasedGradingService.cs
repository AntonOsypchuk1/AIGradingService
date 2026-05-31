using AIGradingService.Api.Models;

namespace AIGradingService.Api.Services;

public class RuleBasedGradingService
{
    public RuleBasedGrade Grade(
        Assignment assignment,
        Submission submission,
        TestRunResult testRun)
    {
        var passRate = testRun.Total == 0
            ? 0
            : (double)testRun.Passed / testRun.Total;

        var correctnessScore = Math.Round(passRate * 5, 2);

        var hasRuntimeError = testRun.HasRuntimeError;
        var qualityScore = hasRuntimeError ? 0 : EstimateCodeQuality(submission.StudentCode);
        var completenessScore = EstimateCompleteness(submission.StudentCode, passRate);

        var predictedScore = correctnessScore + completenessScore + qualityScore;
        predictedScore = Math.Round(Math.Min(predictedScore, assignment.MaxScore), 2);

        var riskFlags = new List<string>();

        if (hasRuntimeError)
            riskFlags.Add("runtime_error");

        if (passRate < 1)
            riskFlags.Add("failed_tests");

        if (submission.StudentCode.Length < 40)
            riskFlags.Add("too_short_solution");

        riskFlags.Add("teacher_must_review");

        return new RuleBasedGrade
        {
            SubmissionId = submission.SubmissionId,
            AssignmentId = submission.AssignmentId,
            MaxScore = assignment.MaxScore,
            ExpectedScore = submission.ExpectedScore,
            PredictedScore = predictedScore,
            AbsoluteError = Math.Abs(predictedScore - submission.ExpectedScore),
            PassedTests = testRun.Passed,
            TotalTests = testRun.Total,
            RiskFlags = riskFlags,
            Criteria =
            [
                new CriterionScore
                {
                    Name = "correctness",
                    Score = correctnessScore,
                    MaxScore = 5,
                    Explanation = $"Passed {testRun.Passed} out of {testRun.Total} tests."
                },
                new CriterionScore
                {
                    Name = "completeness",
                    Score = completenessScore,
                    MaxScore = 3,
                    Explanation = "Estimated from test coverage and solution structure."
                },
                new CriterionScore
                {
                    Name = "code_quality",
                    Score = qualityScore,
                    MaxScore = 2,
                    Explanation = "Estimated from basic code quality heuristics."
                }
            ]
        };
    }

    private static double EstimateCompleteness(string code, double passRate)
    {
        if (passRate >= 1.0)
            return 3;

        if (passRate >= 0.75)
            return 2;

        if (passRate >= 0.5)
            return 1.5;

        if (passRate > 0)
            return 1;

        return 0;
    }

    private static double EstimateCodeQuality(string code)
    {
        var score = 2.0;

        if (code.Contains("while True"))
            score -= 0.5;

        if (code.Length < 40)
            score -= 1;

        if (!code.Contains("input"))
            score -= 0.5;

        return Math.Max(0, score);
    }
}