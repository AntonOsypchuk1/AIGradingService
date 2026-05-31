using AIGradingService.Api.Models;
using AIGradingService.Api.Models.Evaluation;
using CriterionScore = AIGradingService.Api.Models.Evaluation.CriterionScore;

namespace AIGradingService.Api.Services.Baselines;

public class EqualWeightRuleBasedBaseline : IEvaluationBaseline
{
    public string Name => "equal_weight_rule_based";

    public Task<EvaluationGrade> EvaluateAsync(
        BaselineEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var assignment = context.Assignment;
        var submission = context.Submission;
        var testRun = context.TestRun;

        var passRate = testRun.Total == 0
            ? 0
            : (double)testRun.Passed / testRun.Total;

        var correctnessScore = Math.Round(passRate * 5, 2);
        var completenessScore = EstimateCompleteness(passRate);
        var qualityScore = testRun.HasRuntimeError
            ? 0
            : EstimateCodeQuality(submission.StudentCode);

        var predictedScore = correctnessScore + completenessScore + qualityScore;
        predictedScore = Math.Round(Math.Min(predictedScore, assignment.MaxScore), 2);

        var riskFlags = BuildRiskFlags(testRun, submission.StudentCode, passRate);

        var grade = new EvaluationGrade
        {
            BaselineName = Name,

            SubmissionId = submission.SubmissionId,
            AssignmentId = submission.AssignmentId,

            MaxScore = assignment.MaxScore,
            ExpectedScore = submission.ExpectedScore,

            PredictedScore = predictedScore,
            AbsoluteError = Math.Abs(predictedScore - submission.ExpectedScore),

            PassedTests = testRun.Passed,
            TotalTests = testRun.Total,

            PassedWeight = testRun.Passed,
            TotalWeight = testRun.Total,
            PassRate = Math.Round(passRate, 4),

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

        return Task.FromResult(grade);
    }

    private static double EstimateCompleteness(double passRate)
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

    private static List<string> BuildRiskFlags(
        TestRunResult testRun,
        string code,
        double passRate)
    {
        var riskFlags = new List<string>();

        if (testRun.HasRuntimeError)
            riskFlags.Add("runtime_error");

        if (passRate < 1)
            riskFlags.Add("failed_tests");

        if (code.Length < 40)
            riskFlags.Add("too_short_solution");

        riskFlags.Add("teacher_must_review");

        return riskFlags;
    }
}