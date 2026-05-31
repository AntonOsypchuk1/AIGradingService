using AIGradingService.Api.Models.Evaluation;

namespace AIGradingService.Api.Services.StaticAnalysis;

public class PythonStaticAnalysisService
{
    public StaticAnalysisResult Analyze(string code, string requiredConceptsRaw)
    {
        var requiredConcepts = requiredConceptsRaw
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .Distinct()
            .ToList();

        var found = new List<string>();
        var missing = new List<string>();
        var warnings = new List<string>();

        foreach (var concept in requiredConcepts)
        {
            if (IsConceptPresent(code, concept))
                found.Add(concept);
            else
                missing.Add(concept);
        }

        AddWarnings(code, warnings);

        var total = requiredConcepts.Count;
        var matched = found.Count;

        return new StaticAnalysisResult
        {
            MatchedConcepts = matched,
            TotalConcepts = total,
            ConceptCoverage = total == 0 ? 0 : Math.Round((double)matched / total, 4),
            FoundConcepts = found,
            MissingConcepts = missing,
            Warnings = warnings
        };
    }

    private static bool IsConceptPresent(string code, string concept)
    {
        var normalized = code.ToLowerInvariant();

        return concept switch
        {
            "input" => normalized.Contains("input("),
            "if" => normalized.Contains("if "),
            "elif" => normalized.Contains("elif "),
            "else" => normalized.Contains("else"),
            "loop" => normalized.Contains("for ") || normalized.Contains("while "),
            "for" => normalized.Contains("for "),
            "while" => normalized.Contains("while "),
            "modulo" => normalized.Contains("%"),
            "and_or" => normalized.Contains(" and ") || normalized.Contains(" or "),
            "comparison" => normalized.Contains(">") || normalized.Contains("<") || normalized.Contains("=="),
            "arithmetic" => normalized.Contains("+") || normalized.Contains("-") || normalized.Contains("*") || normalized.Contains("/"),
            "split" => normalized.Contains(".split(") || normalized.Contains("split("),
            "counting" => normalized.Contains("count") || normalized.Contains("+="),
            "string_search" => normalized.Contains(".find(") || normalized.Contains(".count(") || normalized.Contains(" in "),
            "list" => normalized.Contains("[") && normalized.Contains("]") || normalized.Contains(".append("),
            "max" => normalized.Contains("max("),
            "reverse" => normalized.Contains(".reverse(") || normalized.Contains("[::-1]") || normalized.Contains("reversed("),
            _ => false
        };
    }

    private static void AddWarnings(string code, List<string> warnings)
    {
        var normalized = code.ToLowerInvariant();

        if (code.Length < 40)
            warnings.Add("too_short_solution");

        if (!normalized.Contains("input("))
            warnings.Add("no_input_usage");

        if (normalized.Contains("print(\"") && !normalized.Contains("input("))
            warnings.Add("possible_hardcoded_output");

        if (normalized.Contains("eval(") || normalized.Contains("exec("))
            warnings.Add("unsafe_dynamic_execution");

        if (normalized.Contains("import os") || normalized.Contains("import subprocess"))
            warnings.Add("potentially_unsafe_import");

        if (normalized.Contains("while true"))
            warnings.Add("possible_infinite_loop");
    }
}