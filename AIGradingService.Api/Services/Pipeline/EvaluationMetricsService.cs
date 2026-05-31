using AIGradingService.Api.Models.Evaluation;

namespace AIGradingService.Api.Services.Pipeline;

public class EvaluationMetricsService
{
    public BatchEvaluationSummary BuildSummary(
        string baselineName,
        List<EvaluationGrade> grades)
    {
        return new BatchEvaluationSummary
        {
            BaselineName = baselineName,
            TotalSubmissions = grades.Count,

            AverageAbsoluteError = grades.Count == 0
                ? 0
                : Math.Round(grades.Average(x => x.AbsoluteError), 2),

            ExactMatchAccuracy = grades.Count == 0
                ? 0
                : Math.Round((double)grades.Count(x => x.AbsoluteError == 0) / grades.Count, 2),

            WithinOneAccuracy = grades.Count == 0
                ? 0
                : Math.Round((double)grades.Count(x => x.AbsoluteError <= 1) / grades.Count, 2),

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