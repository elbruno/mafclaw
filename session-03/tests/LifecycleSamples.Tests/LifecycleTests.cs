// Objective: run the offline regression matrix and make failures visible.
// A. Register focused cases. B. Execute independent deterministic tests. C. Report a process exit status.
namespace MafClaw.LifecycleSamples.Tests;

internal static class LifecycleTests
{
    public static async Task<int> RunAsync()
    {
        (string Name, Func<Task> Run)[] tests =
        [
            ("45 real MAF dispatch + responsive input while both worker inferences block", JobTests.ResponsiveMafConsoleAsync),
            ("45 deterministic ids, queue, concurrency/capacity, unknown ids, pending and repeat collect", JobTests.CapacityAndCollectionAsync),
            ("45 cancellation request differs from observed cancellation and late success", JobTests.CancellationBoundaryAsync),
            ("45 shutdown drains late failure and rejects new admissions", JobTests.ShutdownAndFaultsAsync),
            ("45 distinct sessions for concurrent runs of the same real MAF agent", JobTests.DistinctMafSessionsAsync),
            ("45 cancellation callback errors are observed", JobTests.ThrowingCancellationCallbackAsync),
            ("45 completion/cancel races always settle truthfully", JobTests.CompletionRacesAsync),
            ("46 success survives failure and known transient retry", DeadlineTests.PartialSuccessAndRetryAsync),
            ("46 retries capped; permanent/unclassified/empty outcomes are never success", DeadlineTests.RetryClassificationAsync),
            ("46 noncooperative deadline returns before late fault; disposal drains", DeadlineTests.NoncooperativeDeadlineAsync),
            ("46 explicit cancellation is not reported as deadline or completed work", DeadlineTests.CancellationAsync),
            ("46 queued deadline has zero attempts and never invokes the worker", DeadlineTests.QueuedDeadlineAsync),
            ("46 real MAF tool routing preserves typed report against false narrative", DeadlineTests.MafRoutingAsync),
            ("46 throwing cancellation callbacks do not block a deadline report", DeadlineTests.ThrowingCallbackAsync),
            ("46 blocking cancellation callbacks remain owned without blocking the report", DeadlineTests.BlockingCallbackAsync),
            ("46 already-expired work never begins an attempt", DeadlineTests.ExpiredAdmissionAsync),
            ("45/46 CLI defaults and invalid flags", CommandTests.ContractAsync),
            ("45/46 full fixture prompt/demo output and no interactive input", CommandTests.FixtureEntryPointsAsync),
            ("45/46 unknown CLI modes/arguments return explicit safe failures", CommandTests.InvalidEntryPointsAsync)
        ];
        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception exception)
            {
                failed++;
                Console.WriteLine($"FAIL {test.Name}: {exception.GetType().Name}: {exception.Message}");
            }
        }
        Console.WriteLine($"{tests.Length - failed}/{tests.Length} lifecycle tests passed. No live services used.");
        return failed == 0 ? 0 : 1;
    }
}
