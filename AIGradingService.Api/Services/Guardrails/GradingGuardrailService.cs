using AIGradingService.Api.Models;
using AIGradingService.Api.Models.Evaluation;

namespace AIGradingService.Api.Services.Guardrails;

public class GradingGuardrailService
{
    public GuardrailResult Apply(
        double llmScore,
        TestRunResult testRun,
        List<string> riskFlags)
    {
        var result = new GuardrailResult
        {
            OriginalScore = llmScore,
            FinalScore = llmScore
        };

        var caps = new List<(double Cap, string Rule)>();

        if (testRun.WeightedPassRate == 0)
        {
            caps.Add((2, "weighted_pass_rate_zero_cap_2"));
        }

        if (testRun.HasRuntimeError)
        {
            caps.Add((4, "runtime_error_cap_4"));
        }

        if (testRun.WeightedPassRate < 0.5)
        {
            caps.Add((5, "weighted_pass_rate_below_0_5_cap_5"));
        }

        if (testRun.WeightedPassRate < 0.75)
        {
            caps.Add((8, "weighted_pass_rate_below_0_75_cap_8"));
        }

        if (riskFlags.Any(x =>
                x.Contains("syntax", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("nameerror", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("runtime", StringComparison.OrdinalIgnoreCase)))
        {
            caps.Add((4, "llm_detected_runtime_or_syntax_issue_cap_4"));
        }

        if (caps.Count == 0)
            return result;

        var strictestCap = caps.Min(x => x.Cap);

        result.AppliedCap = strictestCap;
        result.AppliedRules = caps
            .Where(x => x.Cap == strictestCap)
            .Select(x => x.Rule)
            .Distinct()
            .ToList();

        result.FinalScore = Math.Min(llmScore, strictestCap);
        result.FinalScore = Math.Round(result.FinalScore, 2);

        return result;
    }
}