using CsvHelper.Configuration.Attributes;

namespace AIGradingService.Api.Models;

public class Assignment
{
    [Name("assignment_id")]
    public string AssignmentId { get; set; } = string.Empty;

    [Name("lab_id")]
    public string LabId { get; set; } = string.Empty;

    [Name("topic")]
    public string Topic { get; set; } = string.Empty;

    [Name("task_text")]
    public string TaskText { get; set; } = string.Empty;

    [Name("max_score")]
    public int MaxScore { get; set; }
    
    [Name("required_concepts")]
    public string RequiredConcepts { get; set; } = string.Empty;
}