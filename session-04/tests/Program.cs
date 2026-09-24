// Objective: execute deterministic Session 4 regressions with a process-level result.
// A. Run the contract, MAF, privacy and HTTP checks.
// B. Print a summary only after all assertions pass.
using MafClaw.Session04.Tests;

try
{
    if (args.SequenceEqual(["--sample12"]))
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await AspireTelemetryChecks.RunAsync(deadline.Token);
        return 0;
    }
    var count = await Session04Tests.RunAsync();
    Console.WriteLine($"SESSION04 TESTS PASS: {count} checks. No model/service calls.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"SESSION04 TESTS FAIL: {exception.GetType().Name}: {exception.Message}");
    return 1;
}
