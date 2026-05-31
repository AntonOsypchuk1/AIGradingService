using AIGradingService.Api.Models;
using AIGradingService.Api.Models.Evaluation;
using AIGradingService.Api.Services.StaticAnalysis;
using CriterionScore = AIGradingService.Api.Models.Evaluation.CriterionScore;

namespace AIGradingService.Api.Services.Baselines;

public class StaticAnalysisRuleBasedBaseline : IEvaluationBaseline
{
    private readonly PythonStaticAnalysisService _staticAnalysisService;

    public StaticAnalysisRuleBasedBaseline(PythonStaticAnalysisService staticAnalysisService)
    {
        _staticAnalysisService = staticAnalysisService;
    }

    public string Name => "static_analysis_rule_based";

    public Task<EvaluationGrade> EvaluateAsync(
        BaselineEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var assignment = context.Assignment;
        var submission = context.Submission;
        var testRun = context.TestRun;

        var analysis = _staticAnalysisService.Analyze(
            submission.StudentCode,
            assignment.RequiredConcepts);

        var conceptScore = Math.Round(analysis.ConceptCoverage * 5, 2);
        var safetyScore = CalculateSafetyScore(analysis);
        var qualityScore = testRun.HasRuntimeError ? 0 : EstimateCodeQuality(submission.StudentCode);

        var predictedScore = conceptScore + safetyScore + qualityScore;
        predictedScore = Math.Round(Math.Min(predictedScore, assignment.MaxScore), 2);

        var riskFlags = BuildRiskFlags(analysis, testRun);

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

            PassedWeight = testRun.PassedWeight,
            TotalWeight = testRun.TotalWeight,
            PassRate = Math.Round(testRun.WeightedPassRate, 4),

            RiskFlags = riskFlags,

            Criteria =
            [
                new CriterionScore
                {
                    Name = "concept_coverage",
                    Score = conceptScore,
                    MaxScore = 5,
                    Explanation = $"Found {analysis.MatchedConcepts} out of {analysis.TotalConcepts} required concepts. Missing: {string.Join(", ", analysis.MissingConcepts)}."
                },
                new CriterionScore
                {
                    Name = "safety",
                    Score = safetyScore,
                    MaxScore = 3,
                    Explanation = analysis.Warnings.Count == 0
                        ? "No major static-analysis warnings detected."
                        : $"Warnings: {string.Join(", ", analysis.Warnings)}."
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

    private static double CalculateSafetyScore(StaticAnalysisResult analysis)
    {
        var score = 3.0;

        foreach (var warning in analysis.Warnings)
        {
            score -= warning switch
            {
                "unsafe_dynamic_execution" => 2.0,
                "potentially_unsafe_import" => 1.5,
                "possible_infinite_loop" => 1.0,
                "possible_hardcoded_output" => 1.0,
                "no_input_usage" => 1.0,
                "too_short_solution" => 0.5,
                _ => 0.5
            };
        }

        return Math.Max(0, Math.Round(score, 2));
    }

    private static double EstimateCodeQuality(string code)
    {
        var score = 2.0;

        if (code.Length < 40)
            score -= 1;

        if (code.Contains("while True"))
            score -= 0.5;

        if (code.Split('\n').Length <= 2)
            score -= 0.5;

        return Math.Max(0, Math.Round(score, 2));
    }

    private static List<string> BuildRiskFlags(
        StaticAnalysisResult analysis,
        TestRunResult testRun)
    {
        var flags = new List<string>();

        if (analysis.ConceptCoverage < 0.6)
            flags.Add("low_concept_coverage");

        if (analysis.MissingConcepts.Count > 0)
            flags.Add("missing_required_concepts");

        foreach (var warning in analysis.Warnings)
            flags.Add(warning);

        if (testRun.HasRuntimeError)
            flags.Add("runtime_error");

        flags.Add("teacher_must_review");

        return flags.Distinct().ToList();
    }
}