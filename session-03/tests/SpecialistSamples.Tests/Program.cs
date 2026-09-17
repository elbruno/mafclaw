// Objective: run focused offline tests through the installed MAF SDK.
// A. Inspect provider contracts and exact read-only tool allowlists.
// B. Exercise real fan-out, fan-in, selection, budgets and cleanup.
// C. Return nonzero for any regression, without a test-framework dependency.

using MafClaw.SpecialistSamples.Tests;

var tests = new (string Name, Func<Task> Run)[]
{
    ("SDK schemas, capabilities and terminal release", ProviderContractTests.RunAsync),
    ("42: real concurrent specialists and deterministic results", SpecialistTests.Morning42Async),
    ("43: zero/one/two/three worker dispatch subsets", SpecialistTests.Selective43Async),
    ("42 and 43: live-injection path has no keyword router", SpecialistTests.LiveSelectionAsync),
    ("42 and 43: distinct live/fixture worker capability allowlists", NewsCapabilityTests.RunAsync),
    ("42 and 43: live classroom instructions use bundled mock holdings", PortfolioContextTests.RunAsync),
    ("42 and 43: returned hosted-search events and sources, no network", HostedNewsEvidenceTests.RunAsync),
    ("42 and 43: no-dispatch and wrong-subset claims remain incomplete", CompletionTests.FalseClaimsAsync),
    ("42 and 43: uncollected and Failed terminal tasks remain incomplete", CompletionTests.TerminalOutcomesAsync),
    ("42 and 43: expected errors sanitized, unexpected/fatal errors propagate", CompletionTests.ExceptionFilterAsync),
    ("42 and 43: host iteration limit", BoundaryTests.IterationLimitAsync),
    ("42 and 43: deadline cancels and awaits child sessions", BoundaryTests.DeadlineAsync),
    ("42 and 43: failure sanitization and cleanup", BoundaryTests.FailureAsync),
    ("42 and 43: CLI modes, finite demos, prompt and REPL exit", CliTests.RunAsync),
    ("42 and 43: isolated missing-config failure without credential reads", MissingConfigurationTests.RunAsync)
};
int failures = 0;
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {test.Name}: {exception.GetType().Name}: {exception.Message}");
    }
}
Console.WriteLine($"{tests.Length - failures}/{tests.Length} groups passed. Educational mock data only.");
return failures == 0 ? 0 : 1;
