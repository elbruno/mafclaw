// Objective: check console behavior without loading Azure configuration.
// A. Verify live defaults and clear argument errors.
// B. Exercise bounded offline demos, prompts and REPL exit.
// C. Reject unsupported fixture prompts rather than silently use a live model.

using S42 = MafClaw.Sample42;
using S43 = MafClaw.Sample43;

namespace MafClaw.SpecialistSamples.Tests;

public static class CliTests
{
    public static async Task RunAsync()
    {
        Check.True(!S42.CliOptions.Parse([]).Fixture && !S43.CliOptions.Parse([]).Fixture, "No arguments default to live.");
        foreach (int sample in new[] { 42, 43 })
        {
            using var help = new StringWriter();
            Check.Equal(0, await SampleHarness.AppAsync(sample, ["--help"], TextReader.Null, help), "Help exits successfully.");
            Check.True(help.ToString().Contains("--mode live|fixture", StringComparison.Ordinal), "CLI help documents modes.");
            using var demo = new StringWriter();
            Check.Equal(0, await SampleHarness.AppAsync(sample, ["--mode", "fixture", "--demo"], TextReader.Null, demo), "Finite fixture demo.");
            int expectedTurns = sample == 42 ? 1 : 3;
            Check.Equal(expectedTurns, demo.ToString().Split("SESSION_RELEASED", StringSplitOptions.None).Length - 1,
                "Every fixed demo turn releases its sessions and exits.");
            using var prompt = new StringWriter();
            Check.Equal(0, await SampleHarness.AppAsync(sample,
                ["--mode", "fixture", "--prompt", S43.ScenarioPrompts.News], TextReader.Null, prompt), "Bounded prompt.");
            using var repl = new StringWriter();
            Check.Equal(0, await SampleHarness.AppAsync(sample, ["--mode", "fixture"], new StringReader("/exit\n"), repl), "REPL /exit.");
            Check.True(!repl.ToString().Contains("TURN_START", StringComparison.Ordinal), "Exit must not invoke inference.");
            foreach (string[] invalid in new[]
            {
                new[] { "--mode", "unknown" },
                new[] { "--unknown" },
                new[] { "--mode", "fixture", "--mode", "fixture" },
                new[] { "--prompt" },
                new[] { "--mode", "fixture", "--demo", "--prompt", "anything" },
                new[] { "--mode", "fixture", "--prompt", "unsupported fixture text" }
            })
            {
                Check.Equal(2, await SampleHarness.AppAsync(sample, invalid, TextReader.Null, TextWriter.Null),
                    "Invalid input must fail explicitly without cloud fallback.");
            }
        }
    }
}
