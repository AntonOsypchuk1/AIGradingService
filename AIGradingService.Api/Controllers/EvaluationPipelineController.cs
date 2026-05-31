using AIGradingService.Api.Services.Pipeline;
using Microsoft.AspNetCore.Mvc;

namespace AIGradingService.Api.Controllers;

[ApiController]
[Route("api/pipeline")]
public class EvaluationPipelineController : ControllerBase
{
    private readonly EvaluationPipelineService _pipelineService;
    private readonly EvaluationMetricsService _metricsService;

    public EvaluationPipelineController(
        EvaluationPipelineService pipelineService,
        EvaluationMetricsService metricsService)
    {
        _pipelineService = pipelineService;
        _metricsService = metricsService;
    }

    [HttpPost("submissions/{submissionId}/baselines/{baselineName}")]
    public async Task<IActionResult> RunSingleBaseline(
        string submissionId,
        string baselineName,
        CancellationToken cancellationToken)
    {
        var result = await _pipelineService.RunSingleAsync(
            submissionId,
            baselineName,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("submissions/{submissionId}/baselines")]
    public async Task<IActionResult> RunSingleAllBaselines(
        string submissionId,
        CancellationToken cancellationToken)
    {
        var results = await _pipelineService.RunSingleAllBaselinesAsync(
            submissionId,
            cancellationToken);

        return Ok(results);
    }

    [HttpPost("dataset/baselines/{baselineName}")]
    public async Task<IActionResult> RunDatasetBaseline(
        string baselineName,
        CancellationToken cancellationToken)
    {
        var grades = await _pipelineService.RunDatasetAsync(
            baselineName,
            cancellationToken);

        var summary = _metricsService.BuildSummary(baselineName, grades);

        return Ok(summary);
    }

    [HttpPost("dataset/baselines")]
    public async Task<IActionResult> RunDatasetAllBaselines(
        CancellationToken cancellationToken)
    {
        var results = await _pipelineService.RunDatasetAllBaselinesAsync(cancellationToken);
        var summaries = _metricsService.BuildSummaries(results);

        return Ok(summaries);
    }
}