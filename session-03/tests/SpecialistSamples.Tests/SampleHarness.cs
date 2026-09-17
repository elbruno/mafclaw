// Objective: exercise both sample entry points with controlled non-network clients.
// A. Select a sample implementation, not a dispatch outcome.
// B. Inject model behavior into actual MAF orchestration.
// C. Normalize evidence for common regression checks.

using Microsoft.Extensions.AI;
using S42 = MafClaw.Sample42;
using S43 = MafClaw.Sample43;

namespace MafClaw.SpecialistSamples.Tests;

public static class SampleHarness
{
    public static IChatClient Script(int sample, string name, string prompt) => sample == 42
        ? new S42.ScriptedInference(name, prompt).CreateClient()
        : new S43.ScriptedInference(name, prompt).CreateClient();

    public static async Task<ObservedTurn> RunAsync(
        int sample, string prompt, Func<string, IChatClient>? clients = null, int iterations = 16,
        TimeSpan? deadline = null, TextWriter? output = null, IChatClient? live = null)
    {
        if (sample == 42)
        {
            var result = await S42.TeamRunner.RunAsync(prompt, live, output ?? TextWriter.Null,
                new S42.RunLimits(iterations, deadline), clientFactory: clients);
            return new(result.Narrative, result.Completed, result.SessionReleased, result.RunningAfterRelease, result.Events)
            {
                ForegroundFinished = result.ForegroundFinished, ExitCode = result.ExitCode
            };
        }
        else
        {
            var result = await S43.TeamRunner.RunAsync(prompt, live, output ?? TextWriter.Null,
                new S43.RunLimits(iterations, deadline), clientFactory: clients);
            return new(result.Narrative, result.Completed, result.SessionReleased, result.RunningAfterRelease, result.Events)
            {
                ForegroundFinished = result.ForegroundFinished, ExitCode = result.ExitCode
            };
        }
    }

    public static Task<int> AppAsync(int sample, string[] args, TextReader input, TextWriter output) => sample == 42
        ? S42.SampleApp.RunAsync(args, input, output)
        : S43.SampleApp.RunAsync(args, input, output);
}
