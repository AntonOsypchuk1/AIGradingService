using AIGradingService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIGradingService.Api.Controllers;

[ApiController]
[Route("api/dataset")]
public class DatasetController : ControllerBase
{
    private readonly DatasetService _datasetService;

    public DatasetController(DatasetService datasetService)
    {
        _datasetService = datasetService;
    }

    [HttpPost("reload")]
    public IActionResult Reload()
    {
        _datasetService.Load();

        return Ok(new
        {
            assignments = _datasetService.Assignments.Count,
            submissions = _datasetService.Submissions.Count,
            testCases = _datasetService.TestCases.Count
        });
    }

    [HttpGet("assignments")]
    public IActionResult GetAssignments()
    {
        return Ok(_datasetService.Assignments);
    }

    [HttpGet("submissions")]
    public IActionResult GetSubmissions()
    {
        return Ok(_datasetService.Submissions.Select(x => new
        {
            x.SubmissionId,
            x.AssignmentId,
            x.VariantType,
            x.ExpectedScore,
            CodePreview = x.StudentCode.Length > 120
                ? x.StudentCode[..120] + "..."
                : x.StudentCode
        }));
    }

    [HttpGet("test-cases")]
    public IActionResult GetTestCases()
    {
        return Ok(_datasetService.TestCases);
    }
}