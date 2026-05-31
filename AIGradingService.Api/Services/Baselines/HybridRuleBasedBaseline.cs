using AIGradingService.Api.Models;
using AIGradingService.Api.Models.Evaluation;
using AIGradingService.Api.Services.StaticAnalysis;
using CriterionScore = AIGradingService.Api.Models.Evaluation.CriterionScore;

namespace AIGradingService.Api.Services.Baselines;

public class HybridRuleBasedBaseline : IEvaluationBaseline
{
    private readonly PythonStaticAnalysisService _staticAnalysisService;

    public HybridRuleBasedBaseline(PythonStaticAnalysisService staticAnalysisService)
    {
        _staticAnalysisService = staticAnalysisService;
    }

    public string Name => "hybrid_rule_based";

    public Task<EvaluationGrade> EvaluateAsync(
        BaselineEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var assignment = context.Assignment;
        var submission = context.Submission;
        var testRun = context.TestRun;

        var weightedPassRate = testRun.TotalWeight == 0
            ? 0
            : testRun.PassedWeight / testRun.TotalWeight;

        var analysis = _staticAnalysisService.Analyze(
            submission.StudentCode,
            assignment.RequiredConcepts);

        var correctnessScore = Math.Round(weightedPassRate * 5, 2);
        var completenessScore = EstimateCompleteness(weightedPassRate);
        var qualityScore = testRun.HasRuntimeError
            ? 0
            : EstimateCodeQuality(submission.StudentCode);

        var baseScore = correctnessScore + completenessScore + qualityScore;

        var penalty = CalculateStaticPenalty(analysis, testRun);
        var predictedScore = Math.Round(Math.Max(0, Math.Min(baseScore - penalty, assignment.MaxScore)), 2);

        var riskFlags = BuildRiskFlags(analysis, testRun, penalty);

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

            PassedWeight = Math.Round(testRun.PassedWeight, 2),
            TotalWeight = Math.Round(testRun.TotalWeight, 2),
            PassRate = Math.Round(weightedPassRate, 4),

            RiskFlags = riskFlags,

            Criteria =
            [
                new CriterionScore
                {
                    Name = "weighted_correctness",
                    Score = correctnessScore,
                    MaxScore = 5,
                    Explanation = $"Passed weighted score {testRun.PassedWeight:0.##} out of {testRun.TotalWeight:0.##}."
                },
                new CriterionScore
                {
                    Name = "completeness",
                    Score = completenessScore,
                    MaxScore = 3,
                    Explanation = "Estimated from weighted test coverage."
                },
                new CriterionScore
                {
                    Name = "code_quality",
                    Score = qualityScore,
                    MaxScore = 2,
                    Explanation = "Estimated from basic code quality heuristics."
                },
                new CriterionScore
                {
                    Name = "static_analysis_penalty",
                    Score = -penalty,
                    MaxScore = 0,
                    Explanation =
                        $"Static analysis found {analysis.MatchedConcepts}/{analysis.TotalConcepts} concepts. " +
                        $"Missing: {string.Join(", ", analysis.MissingConcepts)}. " +
                        $"Warnings: {string.Join(", ", analysis.Warnings)}."
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

        return Math.Max(0, Math.Round(score, 2));
    }

    private static double CalculateStaticPenalty(
        StaticAnalysisResult analysis,
        TestRunResult testRun)
    {
        var penalty = 0.0;

        // Missing concepts should matter, but not dominate test results.
        if (analysis.ConceptCoverage < 0.8)
            penalty += 0.5;

        if (analysis.ConceptCoverage < 0.6)
            penalty += 1.0;

        if (analysis.ConceptCoverage < 0.4)
            penalty += 1.5;

        foreach (var warning in analysis.Warnings)
        {
            penalty += warning switch
            {
                "unsafe_dynamic_execution" => 2.0,
                "potentially_unsafe_import" => 1.5,
                "possible_infinite_loop" => 1.0,
                "possible_hardcoded_output" => 1.0,
                "no_input_usage" => 1.0,
                "too_short_solution" => 0.5,
                _ => 0.25
            };
        }

        // Runtime error should be serious, but test score already captures part of it.
        if (testRun.HasRuntimeError)
            penalty += 1.0;

        return Math.Round(Math.Min(penalty, 3.0), 2);
    }

    private static List<string> BuildRiskFlags(
        StaticAnalysisResult analysis,
        TestRunResult testRun,
        double penalty)
    {
        var flags = new List<string>();

        if (testRun.WeightedPassRate < 1)
            flags.Add("failed_weighted_tests");

        if (analysis.MissingConcepts.Count > 0)
            flags.Add("missing_required_concepts");

        if (analysis.ConceptCoverage < 0.6)
            flags.Add("low_concept_coverage");

        foreach (var warning in analysis.Warnings)
            flags.Add(warning);

        if (testRun.HasRuntimeError)
            flags.Add("runtime_error");

        if (penalty > 0)
            flags.Add("static_penalty_applied");

        flags.Add("teacher_must_review");

        return flags.Distinct().ToList();
    }
}