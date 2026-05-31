using System.Diagnostics;
using AIGradingService.Api.Models;

namespace AIGradingService.Api.Services;

public class PythonExecutionService
{
    private const int TimeoutMs = 3000;

    public async Task<TestRunResult> RunSubmissionTestsAsync(
        Submission submission,
        List<TestCase> testCases,
        CancellationToken cancellationToken = default)
    {
        var result = new TestRunResult
        {
            SubmissionId = submission.SubmissionId,
            AssignmentId = submission.AssignmentId,
            Total = testCases.Count
        };

        foreach (var testCase in testCases)
        {
            var caseResult = await RunSingleTestAsync(submission.StudentCode, testCase, cancellationToken);

            result.Results.Add(caseResult);

            if (caseResult.Passed)
                result.Passed++;
        }

        return result;
    }

    private static async Task<TestCaseResult> RunSingleTestAsync(
        string code,
        TestCase testCase,
        CancellationToken cancellationToken)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"submission_{Guid.NewGuid():N}.py");

        await File.WriteAllTextAsync(tempFile, code, cancellationToken);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{tempFile}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            await process.StandardInput.WriteAsync(testCase.InputData);
            process.StandardInput.Close();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            var exited = await WaitForExitAsync(process, TimeoutMs, cancellationToken);

            if (!exited)
            {
                TryKill(process);

                return new TestCaseResult
                {
                    Input = testCase.InputData,
                    ExpectedOutput = Normalize(testCase.ExpectedOutput),
                    ActualOutput = "",
                    Error = "Execution timeout.",
                    Passed = false
                };
            }

            var output = await outputTask;
            var error = await errorTask;

            var actual = Normalize(output);
            var expected = Normalize(testCase.ExpectedOutput);

            return new TestCaseResult
            {
                Input = testCase.InputData,
                ExpectedOutput = expected,
                ActualOutput = actual,
                Error = error.Trim(),
                Passed = string.IsNullOrWhiteSpace(error) && IsOutputCorrect(actual, expected)
            };
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
    
    private static bool IsOutputCorrect(string actual, string expected)
    {
        actual = Normalize(actual).ToLowerInvariant();
        expected = Normalize(expected).ToLowerInvariant();

        var expectedParts = expected
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (expectedParts.Length == 0)
            return false;

        return expectedParts.All(part => actual.Contains(part));
    }

    private static async Task<bool> WaitForExitAsync(
        Process process,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        var waitTask = process.WaitForExitAsync(cancellationToken);
        var delayTask = Task.Delay(timeoutMs, cancellationToken);

        var completedTask = await Task.WhenAny(waitTask, delayTask);

        return completedTask == waitTask;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignore
        }
    }

    private static string Normalize(string value)
    {
        return value
            .Replace("\r", "")
            .Trim();
    }
}