using AIGradingService.Api.Models;
using AIGradingService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIGradingService.Api.Controllers;

[ApiController]
[Route("api/evaluations")]
public class EvaluationController : ControllerBase
{
    private readonly DatasetService _datasetService;
    private readonly PythonExecutionService _pythonExecutionService;
    private readonly RuleBasedGradingService _ruleBasedGradingService;

    public EvaluationController(
        DatasetService datasetService,
        PythonExecutionService pythonExecutionService,
        RuleBasedGradingService ruleBasedGradingService)
    {
        _datasetService = datasetService;
        _pythonExecutionService = pythonExecutionService;
        _ruleBasedGradingService = ruleBasedGradingService;
    }

    [HttpPost("{submissionId}/run-tests")]
    public async Task<IActionResult> RunTests(string submissionId, CancellationToken cancellationToken)
    {
        var submission = _datasetService.GetSubmission(submissionId);

        if (submission is null)
            return NotFound(new { message = "Submission not found." });

        var assignment = _datasetService.GetAssignment(submission.AssignmentId);

        if (assignment is null)
            return NotFound(new { message = "Assignment not found." });

        var testCases = _datasetService.GetTestCases(submission.AssignmentId);

        if (testCases.Count == 0)
            return BadRequest(new { message = "No test cases found for assignment." });

        var result = await _pythonExecutionService.RunSubmissionTestsAsync(
            submission,
            testCases,
            cancellationToken);

        return Ok(new
        {
            assignment,
            submission = new
            {
                submission.SubmissionId,
                submission.AssignmentId,
                submission.VariantType,
                submission.ExpectedScore
            },
            testRun = result
        });
    }
    
    [HttpPost("{submissionId}/grade-rule-based")]
    public async Task<IActionResult> GradeRuleBased(
        string submissionId,
        CancellationToken cancellationToken)
    {
        var submission = _datasetService.GetSubmission(submissionId);

        if (submission is null)
            return NotFound(new { message = "Submission not found." });

        var assignment = _datasetService.GetAssignment(submission.AssignmentId);

        if (assignment is null)
            return NotFound(new { message = "Assignment not found." });

        var testCases = _datasetService.GetTestCases(submission.AssignmentId);

        if (testCases.Count == 0)
            return BadRequest(new { message = "No test cases found for assignment." });

        var testRun = await _pythonExecutionService.RunSubmissionTestsAsync(
            submission,
            testCases,
            cancellationToken);

        var grade = _ruleBasedGradingService.Grade(
            assignment,
            submission,
            testRun);

        return Ok(new
        {
            assignment,
            submission = new
            {
                submission.SubmissionId,
                submission.AssignmentId,
                submission.VariantType,
                submission.ExpectedScore
            },
            testRun,
            grade
        });
    }
    
    [HttpPost("run-all-rule-based")]
    public async Task<IActionResult> RunAllRuleBased(CancellationToken cancellationToken)
    {
        var grades = new List<RuleBasedGrade>();

        foreach (var submission in _datasetService.Submissions)
        {
            var assignment = _datasetService.GetAssignment(submission.AssignmentId);

            if (assignment is null)
                continue;

            var testCases = _datasetService.GetTestCases(submission.AssignmentId);

            if (testCases.Count == 0)
                continue;

            var testRun = await _pythonExecutionService.RunSubmissionTestsAsync(
                submission,
                testCases,
                cancellationToken);

            var grade = _ruleBasedGradingService.Grade(
                assignment,
                submission,
                testRun);

            grades.Add(grade);
        }

        var result = new BatchEvaluationResult
        {
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

        return Ok(result);
    }
}