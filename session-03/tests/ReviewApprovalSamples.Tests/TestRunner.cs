// Objective: run every focused check and clean only this run's test-owned files.
// A. Allocate a unique directory underneath this test executable, never a temp directory.
// B. Report concise pass/fail results for offline real-MAF checks.
// C. Delete only this test run's root and return an aggregate exit status.

namespace MafClaw.ReviewApprovalSamples.Tests;

internal static class TestRunner
{
    public static async Task<int> RunAsync()
    {
        var root = Path.Combine(AppContext.BaseDirectory, ".test-workspaces", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var tests = OrderedWorkflowTests.Cases().Concat(ApprovalWorkflowTests.Cases(root))
            .Concat(CliContractTests.Cases()).ToArray();
        var failures = 0;
        try
        {
            foreach (var (name, run) in tests)
            {
                try
                {
                    await run();
                    Console.WriteLine($"PASS: {name}");
                }
                catch (Exception exception)
                {
                    failures++;
                    Console.Error.WriteLine($"FAIL: {name}\n{exception.GetType().Name}: {exception.Message}");
                }
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} checks passed — offline scripted inference, actual MAF orchestration.");
        return failures == 0 ? 0 : 1;
    }
}
