using System.Globalization;
using System.Text.Json;
using AIGradingService.Api.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace AIGradingService.Api.Services;

public class DatasetService
{
    private readonly List<Assignment> _assignments = [];
    private readonly List<Submission> _submissions = [];
    private readonly List<TestCase> _testCases = [];

    public IReadOnlyList<Assignment> Assignments => _assignments;
    public IReadOnlyList<Submission> Submissions => _submissions;
    public IReadOnlyList<TestCase> TestCases => _testCases;

    public void Load()
    {
        _assignments.Clear();
        _submissions.Clear();
        _testCases.Clear();

        LoadAssignments();
        LoadSubmissions();
        LoadTestCases();
    }

    private void LoadAssignments()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Dataset", "assignments.csv");

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CreateCsvConfig());

        var records = csv.GetRecords<Assignment>().ToList();
        _assignments.AddRange(records);
    }

    private void LoadTestCases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Dataset", "test_cases.csv");

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CreateCsvConfig());

        var records = csv.GetRecords<TestCase>().ToList();
        _testCases.AddRange(records);
    }

    private void LoadSubmissions()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Dataset", "submissions.jsonl");

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var submission = JsonSerializer.Deserialize<Submission>(
                line,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (submission is not null)
                _submissions.Add(submission);
        }
    }

    public Assignment? GetAssignment(string assignmentId)
    {
        return _assignments.FirstOrDefault(x => x.AssignmentId == assignmentId);
    }

    public Submission? GetSubmission(string submissionId)
    {
        return _submissions.FirstOrDefault(x => x.SubmissionId == submissionId);
    }

    public List<TestCase> GetTestCases(string assignmentId)
    {
        return _testCases
            .Where(x => x.AssignmentId == assignmentId)
            .ToList();
    }

    private static CsvConfiguration CreateCsvConfig()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args =>
                args.Header.Replace("_", "").ToLowerInvariant()
        };
    }
}