using CsvHelper.Configuration.Attributes;

namespace AIGradingService.Api.Models;

public class TestCase
{
    [Name("assignment_id")]
    public string AssignmentId { get; set; } = string.Empty;

    [Name("stdin")]
    public string InputData { get; set; } = string.Empty;

    [Name("expected_stdout_contains")]
    public string ExpectedOutput { get; set; } = string.Empty;
    
    [Name("weight")]
    public double Weight { get; set; } = 1;
}