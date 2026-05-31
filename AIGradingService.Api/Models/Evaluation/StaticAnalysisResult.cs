namespace AIGradingService.Api.Models.Evaluation;

public class StaticAnalysisResult
{
    public int MatchedConcepts { get; set; }
    public int TotalConcepts { get; set; }
    public double ConceptCoverage { get; set; }

    public List<string> FoundConcepts { get; set; } = [];
    public List<string> MissingConcepts { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}