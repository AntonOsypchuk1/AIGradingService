using AIGradingService.Api.Models.Evaluation;
using AIGradingService.Api.Services.Baselines;

namespace AIGradingService.Api.Services.Pipeline;

public class EvaluationPipelineService
{
    private readonly DatasetService _datasetService;
    private readonly PythonExecutionService _pythonExecutionService;
    private readonly IEnumerable<IEvaluationBaseline> _baselines;

    public EvaluationPipelineService(
        DatasetService datasetService,
        PythonExecutionService pythonExecutionService,
        IEnumerable<IEvaluationBaseline> baselines)
    {
        _datasetService = datasetService;
        _pythonExecutionService = pythonExecutionService;
        _baselines = baselines;
    }

    public async Task<EvaluationGrade> RunSingleAsync(
        string submissionId,
        string baselineName,
        CancellationToken cancellationToken = default)
    {
        var baseline = _baselines.FirstOrDefault(x => x.Name == baselineName);

        if (baseline is null)
            throw new InvalidOperationException($"Baseline '{baselineName}' was not found.");

        var context = await BuildContextAsync(submissionId, cancellationToken);

        return await baseline.EvaluateAsync(context, cancellationToken);
    }

    public async Task<List<EvaluationGrade>> RunSingleAllBaselinesAsync(
        string submissionId,
        CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(submissionId, cancellationToken);

        var results = new List<EvaluationGrade>();

        foreach (var baseline in _baselines)
        {
            var grade = await baseline.EvaluateAsync(context, cancellationToken);
            results.Add(grade);
        }

        return results;
    }

    public async Task<List<EvaluationGrade>> RunDatasetAsync(
        string baselineName,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EvaluationGrade>();

        foreach (var submission in _datasetService.Submissions)
        {
            var grade = await RunSingleAsync(
                submission.SubmissionId,
                baselineName,
                cancellationToken);

            results.Add(grade);
        }

        return results;
    }

    public async Task<Dictionary<string, List<EvaluationGrade>>> RunDatasetAllBaselinesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, List<EvaluationGrade>>();

        foreach (var baseline in _baselines)
        {
            var grades = await RunDatasetAsync(baseline.Name, cancellationToken);
            result[baseline.Name] = grades;
        }

        return result;
    }

    private async Task<BaselineEvaluationContext> BuildContextAsync(
        string submissionId,
        CancellationToken cancellationToken)
    {
        var submission = _datasetService.GetSubmission(submissionId);

        if (submission is null)
            throw new InvalidOperationException("Submission not found.");

        var assignment = _datasetService.GetAssignment(submission.AssignmentId);

        if (assignment is null)
            throw new InvalidOperationException("Assignment not found.");

        var testCases = _datasetService.GetTestCases(submission.AssignmentId);

        if (testCases.Count == 0)
            throw new InvalidOperationException("No test cases found for assignment.");

        var testRun = await _pythonExecutionService.RunSubmissionTestsAsync(
            submission,
            testCases,
            cancellationToken);

        return new BaselineEvaluationContext
        {
            Assignment = assignment,
            Submission = submission,
            TestRun = testRun
        };
    }
}