using System.Text.Json.Serialization;

namespace AIGradingService.Api.Models;

public class Submission
{
    [JsonPropertyName("submission_id")]
    public string SubmissionId { get; set; } = string.Empty;

    [JsonPropertyName("assignment_id")]
    public string AssignmentId { get; set; } = string.Empty;

    [JsonPropertyName("variant_type")]
    public string VariantType { get; set; } = string.Empty;

    [JsonPropertyName("student_code")]
    public string StudentCode { get; set; } = string.Empty;

    [JsonPropertyName("expected_score")]
    public int ExpectedScore { get; set; }
}