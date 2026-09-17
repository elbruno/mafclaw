// Objective: protect the shared CLI behavior of two independently runnable samples.
// A. Check default live mode. B. Check scripted/demo/prompt options. C. Reject ambiguity without contacting Azure.
using Options45 = MafClaw.Sample45.CommandOptions;
using Options46 = MafClaw.Sample46.CommandOptions;
using MafClaw.OrchestrationSupport;

namespace MafClaw.LifecycleSamples.Tests;

internal static class CommandTests
{
    public static Task ContractAsync()
    {
        Check.That(Options45.Parse([]) is { Fixture: false, Demo: false, Prompt: null }, "45 default must be live interactive.");
        Check.That(Options46.Parse([]) is { Fixture: false, Demo: false, Prompt: null }, "46 default must be live interactive.");
        Check.That(Options45.Parse(["--mode", "fixture", "--demo"]).Fixture, "45 fixture demo must parse.");
        Check.That(Options46.Parse(["--mode", "fixture", "--prompt", "hello"]).Prompt == "hello", "46 bounded turn must parse.");
        Check.That(MafClaw.Sample46.LiveTiming.BatchDeadline == TimeSpan.FromSeconds(20) &&
            MafClaw.Sample46.LiveTiming.SlowSourceDelay == TimeSpan.FromSeconds(40) &&
            MafClaw.Sample46.LiveTiming.ForegroundBudget == TimeSpan.FromSeconds(60),
            "Live teaching budgets must allow news inference while keeping the injected slow source beyond the batch deadline.");
        foreach (var invalid in new[]
        {
            new[] { "--mode", "unknown" }, new[] { "--prompt" }, new[] { "--prompt", "" },
            new[] { "--demo", "--prompt", "x" }, new[] { "--unknown" }, new[] { "--mode", "live", "--mode", "fixture" }
        })
        {
            Check.Throws<ArgumentException>(() => Options45.Parse(invalid));
            Check.Throws<ArgumentException>(() => Options46.Parse(invalid));
        }
        return Task.CompletedTask;
    }

    public static async Task FixtureEntryPointsAsync()
    {
        (string Sample, Func<string[], Task<int>> Run)[] applications =
        [
            ("45", MafClaw.Sample45.SampleApplication.RunAsync),
            ("46", MafClaw.Sample46.SampleApplication.RunAsync)
        ];
        foreach (var application in applications)
        {
            foreach (var arguments in new[]
            {
                new[] { "--mode", "fixture", "--demo" },
                new[] { "--mode", "fixture", "--prompt", "Start research on fictional ACME." }
            })
            {
                var result = await CaptureAsync(() => SafeConsole.RunAsync(() => application.Run(arguments)));
                Check.That(result.ExitCode == 0 && result.Error.Length == 0,
                    $"{application.Sample} fixture entry point must exit successfully without reading console input.");
                Check.That(result.Output.Contains("SCRIPTED FIXTURE") &&
                    result.Output.Contains("TOOL_CALL") && result.Output.Contains("TOOL_RESULT"),
                    "Both bounded fixture modes must visibly exercise actual MAF tool routing.");
                if (application.Sample == "45")
                {
                    Check.That(result.Output.Contains("Shutdown observed: job-001"),
                        "The one-turn path must drain jobs and print observed shutdown states.");
                    if (arguments.Contains("--demo"))
                    {
                        Check.That(result.Output.Contains("pending (running)") &&
                            result.Output.Contains("job-001 NewsResearchAgent: completed") &&
                            result.Output.Contains("job-002 HoldingsResearchAgent: cancelled"),
                            "Demo output must prove pending, completion and observed cancellation.");
                    }
                }
                else
                {
                    Check.That(result.Output.Contains("Fixture timing: gate-controlled deadline") &&
                        result.Output.Contains("PARTIAL / INCOMPLETE: 1/3") &&
                        result.Output.Contains("UnavailableResearchAgent: failed; attempts=1") &&
                        result.Output.Contains("SlowHoldingsResearchAgent: timed-out; attempts=1") &&
                        result.Output.Contains("Shutdown complete: 0 late worker failures observed"),
                        "Both 46 entry points must preserve success, failure and deadline states through cleanup.");
                }
            }
        }
    }

    public static async Task InvalidEntryPointsAsync()
    {
        Func<string[], Task<int>>[] applications =
        [
            MafClaw.Sample45.SampleApplication.RunAsync,
            MafClaw.Sample46.SampleApplication.RunAsync
        ];
        foreach (var application in applications)
        {
            foreach (var arguments in new[] { new[] { "--mode", "unknown" }, new[] { "--unsupported" } })
            {
                var result = await CaptureAsync(() => SafeConsole.RunAsync(() => application(arguments)));
                Check.That(result.ExitCode != 0 && result.Error.Contains("ArgumentException"),
                    "Unknown modes/arguments must fail explicitly through safe console errors.");
                Check.That(result.Output.Length == 0,
                    "Invalid CLI arguments must stop before model/configuration initialization.");
            }
        }
    }

    private static async Task<(int ExitCode, string Output, string Error)> CaptureAsync(Func<Task<int>> run)
    {
        var originalInput = Console.In;
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var input = new NoInputReader();
        try
        {
            Console.SetIn(input);
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = await run();
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetIn(originalInput);
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }
}
