using System.Text.Json;
using AIGradingService.Api.Models.Evaluation;
using AIGradingService.Api.Services.Guardrails;
using AIGradingService.Api.Services.Llm;

namespace AIGradingService.Api.Services.Baselines;

public class TestAwareGuardedLlmBaseline : IEvaluationBaseline
{
    private readonly ILlmClient _llmClient;
    private readonly GradingGuardrailService _guardrailService;

    public TestAwareGuardedLlmBaseline(
        ILlmClient llmClient,
        GradingGuardrailService guardrailService)
    {
        _llmClient = llmClient;
        _guardrailService = guardrailService;
    }

    public string Name => "test_aware_llm_guarded_grading";

    public async Task<EvaluationGrade> EvaluateAsync(
        BaselineEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var assignment = context.Assignment;
        var submission = context.Submission;
        var testRun = context.TestRun;

        var prompt = BuildPrompt(context);

        var rawResponse = await _llmClient.CompleteAsync(prompt, cancellationToken);

        var llmResult = JsonSerializer.Deserialize<LlmGradeResult>(
            rawResponse,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (llmResult is null)
            throw new InvalidOperationException("Could not parse LLM response.");

        var rawPredictedScore = Math.Round(
            Math.Max(0, Math.Min(llmResult.PredictedScore, assignment.MaxScore)),
            2);

        var riskFlags = llmResult.RiskFlags
            .Append("teacher_must_review")
            .Distinct()
            .ToList();

        var guardrail = _guardrailService.Apply(
            rawPredictedScore,
            testRun,
            riskFlags);

        if (guardrail.AppliedRules.Count > 0)
        {
            riskFlags.Add("guardrail_applied");
            riskFlags.AddRange(guardrail.AppliedRules);
        }

        riskFlags = riskFlags
            .Distinct()
            .ToList();

        var criteria = llmResult.Criteria;

        criteria.Add(new CriterionScore
        {
            Name = "guardrail_adjustment",
            Score = guardrail.FinalScore - guardrail.OriginalScore,
            MaxScore = 0,
            Explanation = guardrail.AppliedRules.Count == 0
                ? "No deterministic guardrail was applied."
                : $"Original LLM score {guardrail.OriginalScore} was capped to {guardrail.FinalScore}. Applied rules: {string.Join(", ", guardrail.AppliedRules)}."
        });

        return new EvaluationGrade
        {
            BaselineName = Name,
            ModelName = _llmClient.ModelName,

            SubmissionId = submission.SubmissionId,
            AssignmentId = submission.AssignmentId,

            MaxScore = assignment.MaxScore,
            ExpectedScore = submission.ExpectedScore,

            PredictedScore = guardrail.FinalScore,
            AbsoluteError = Math.Abs(guardrail.FinalScore - submission.ExpectedScore),

            PassedTests = testRun.Passed,
            TotalTests = testRun.Total,
            PassedWeight = testRun.PassedWeight,
            TotalWeight = testRun.TotalWeight,
            PassRate = Math.Round(testRun.WeightedPassRate, 4),

            Criteria = criteria,
            RiskFlags = riskFlags,
            FinalDecisionPolicy = "teacher_must_confirm"
        };
    }

    private static string BuildPrompt(BaselineEvaluationContext context)
    {
        var assignment = context.Assignment;
        var submission = context.Submission;
        var testRun = context.TestRun;

        var failedCases = testRun.Results
            .Where(x => !x.Passed)
            .Select(x =>
                $"""
                Input: {x.Input}
                Expected output: {x.ExpectedOutput}
                Actual output: {x.ActualOutput}
                Error: {x.Error}
                Weight: {x.Weight}
                """);

        var failedCasesText = string.Join("\n---\n", failedCases);

        if (string.IsNullOrWhiteSpace(failedCasesText))
            failedCasesText = "No failed test cases.";

        return $$"""
        You are an AI assistant for grading introductory Python programming assignments.

        Grade the student's code from 0 to {{assignment.MaxScore}}.

        This is not a final grade. A teacher must review the result.

        Use the rubric:
        - correctness: 0 to 5 points
        - completeness: 0 to 3 points
        - code_quality: 0 to 2 points

        Important grading rules:
        - Use test results as strong evidence.
        - If tests fail, explain which behavior is incorrect.
        - Do not give a high correctness score if important weighted tests fail.
        - If all tests fail, predictedScore should normally be very low.
        - If the code has runtime errors or syntax errors, mark it with a risk flag.
        - The sum of criterion scores must equal predictedScore.
        - Return only raw JSON.
        - Do not use markdown.
        - Do not wrap the response in ```json.

        JSON schema:
        {
          "predictedScore": number,
          "criteria": [
            {
              "name": "correctness",
              "score": number,
              "maxScore": 5,
              "explanation": "short explanation"
            },
            {
              "name": "completeness",
              "score": number,
              "maxScore": 3,
              "explanation": "short explanation"
            },
            {
              "name": "code_quality",
              "score": number,
              "maxScore": 2,
              "explanation": "short explanation"
            }
          ],
          "riskFlags": ["teacher_must_review"],
          "explanation": "overall explanation"
        }

        Assignment id: {{assignment.AssignmentId}}
        Topic: {{assignment.Topic}}

        Task:
        {{assignment.TaskText}}

        Student code:
        ```python
        {{submission.StudentCode}}
        ```

        Test summary:
        Passed tests: {{testRun.Passed}} / {{testRun.Total}}
        Passed weighted score: {{testRun.PassedWeight}} / {{testRun.TotalWeight}}
        Weighted pass rate: {{Math.Round(testRun.WeightedPassRate, 4)}}

        Failed test cases:
        {{failedCasesText}}
        """;
    }
}