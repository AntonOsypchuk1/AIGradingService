using System.Text.Json;
using AIGradingService.Api.Models.Evaluation;

namespace AIGradingService.Api.Services.Llm;

public class FakeLlmClient : ILlmClient
{
    public string ModelName => "fake-llm";
    
    public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var predictedScore = EstimateFakeScore(prompt);

        var result = new LlmGradeResult
        {
            PredictedScore = predictedScore,
            Explanation = "Fake LLM response used for pipeline verification.",
            RiskFlags = ["teacher_must_review", "fake_llm_result"],
            Criteria =
            [
                new CriterionScore
                {
                    Name = "correctness",
                    Score = Math.Min(5, predictedScore / 2),
                    MaxScore = 5,
                    Explanation = "Estimated by fake LLM placeholder."
                },
                new CriterionScore
                {
                    Name = "completeness",
                    Score = Math.Min(3, predictedScore / 3),
                    MaxScore = 3,
                    Explanation = "Estimated by fake LLM placeholder."
                },
                new CriterionScore
                {
                    Name = "code_quality",
                    Score = Math.Min(2, predictedScore / 5),
                    MaxScore = 2,
                    Explanation = "Estimated by fake LLM placeholder."
                }
            ]
        };

        return Task.FromResult(JsonSerializer.Serialize(result));
    }

    private static double EstimateFakeScore(string prompt)
    {
        var normalized = prompt.ToLowerInvariant();

        if (normalized.Contains("syntaxerror") || normalized.Contains("runtime error"))
            return 1;

        if (normalized.Contains("variant_type: correct"))
            return 10;

        if (normalized.Contains("variant_type: partial"))
            return 7;

        if (normalized.Contains("variant_type: wrong"))
            return 3;

        return 5;
    }
}