using AIGradingService.Api.Models.Evaluation;

namespace AIGradingService.Api.Services.Pipeline;

public class EvaluationMetricsService
{
    private const double LargeErrorThreshold = 2.0;

    public BatchEvaluationSummary BuildSummary(
        string baselineName,
        List<EvaluationGrade> grades)
    {
        var successfulGrades = grades
            .Where(x => !x.RiskFlags.Contains("evaluation_failed"))
            .ToList();

        var total = grades.Count;
        var successful = successfulGrades.Count;
        var failed = total - successful;

        if (successful == 0)
        {
            return new BatchEvaluationSummary
            {
                BaselineName = baselineName,
                TotalSubmissions = total,
                FailedEvaluations = failed,
                SuccessRate = total == 0 ? 0 : Math.Round((double)successful / total, 2),
                Grades = grades
            };
        }

        var exactMatchCount = successfulGrades
            .Count(x => x.AbsoluteError == 0);

        var withinOneCount = successfulGrades
            .Count(x => x.AbsoluteError <= 1);

        var largeErrorCount = successfulGrades
            .Count(x => x.AbsoluteError > LargeErrorThreshold);

        var overestimatedCount = successfulGrades
            .Count(x => x.PredictedScore > x.ExpectedScore);

        var underestimatedCount = successfulGrades
            .Count(x => x.PredictedScore < x.ExpectedScore);

        var guardrailAppliedCount = successfulGrades
            .Count(x => x.RiskFlags.Contains("guardrail_applied"));

        return new BatchEvaluationSummary
        {
            BaselineName = baselineName,

            TotalSubmissions = total,
            FailedEvaluations = failed,

            SuccessRate = Math.Round((double)successful / total, 2),

            AverageAbsoluteError = Math.Round(
                successfulGrades.Average(x => x.AbsoluteError),
                2),

            ExactMatchAccuracy = Math.Round(
                (double)exactMatchCount / successful,
                2),

            WithinOneAccuracy = Math.Round(
                (double)withinOneCount / successful,
                2),

            MaxAbsoluteError = Math.Round(
                successfulGrades.Max(x => x.AbsoluteError),
                2),

            LargeErrorRate = Math.Round(
                (double)largeErrorCount / successful,
                2),

            OverestimationRate = Math.Round(
                (double)overestimatedCount / successful,
                2),

            UnderestimationRate = Math.Round(
                (double)underestimatedCount / successful,
                2),

            ExactMatchCount = exactMatchCount,
            WithinOneCount = withinOneCount,
            LargeErrorCount = largeErrorCount,

            OverestimatedCount = overestimatedCount,
            UnderestimatedCount = underestimatedCount,

            AveragePredictedScore = Math.Round(
                successfulGrades.Average(x => x.PredictedScore),
                2),

            AverageExpectedScore = Math.Round(
                successfulGrades.Average(x => x.ExpectedScore),
                2),

            GuardrailAppliedCount = guardrailAppliedCount,

            GuardrailAppliedRate = Math.Round(
                (double)guardrailAppliedCount / successful,
                2),

            Grades = grades
        };
    }

    public List<BatchEvaluationSummary> BuildSummaries(
        Dictionary<string, List<EvaluationGrade>> results)
    {
        return results
            .Select(x => BuildSummary(x.Key, x.Value))
            .ToList();
    }
}