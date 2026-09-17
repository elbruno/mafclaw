// Objective: challenge host budgets, cooperative cleanup and safe failures.
// A. Use an endless scripted model to exercise the real Harness iteration cap.
// B. Stall real child runs until the host deadline forces terminal release.
// C. Ensure raw worker error details never enter returned evidence.

using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;
using Prompts = MafClaw.Sample43.ScenarioPrompts;

namespace MafClaw.SpecialistSamples.Tests;

public static class BoundaryTests
{
    public static async Task IterationLimitAsync()
    {
        foreach (int sample in new[] { 42, 43 })
        {
            int rounds = 0, workerCalls = 0;
            IChatClient Factory(string name) => new FixtureChatClient((messages, options, cancellationToken) =>
            {
                if (name != "MainAgent")
                {
                    Interlocked.Increment(ref workerCalls);
                }
                int round = Interlocked.Increment(ref rounds);
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent($"loop-{round}", "background_agents_get_all_tasks", new Dictionary<string, object?>())])));
            });
            var result = await SampleHarness.RunAsync(sample, "Endless model fixture", Factory,
                iterations: 2, deadline: TimeSpan.FromSeconds(5));
            Check.True(rounds <= 3, "Harness must stop the non-cooperating model's function loop.");
            Check.True(!result.Completed && result.SessionReleased, "Iteration exhaustion is incomplete and cleaned up.");
            Check.Equal(0, workerCalls, "Inspection loop must not create workers.");
            Check.Equal(0, result.RunningAfterRelease, "No running work remains.");
        }
    }

    public static async Task DeadlineAsync()
    {
        foreach (int sample in new[] { 42, 43 })
        {
            int started = 0, cancelled = 0;
            IChatClient Factory(string name) => name == "MainAgent"
                ? SampleHarness.Script(sample, name, Prompts.MorningBrief)
                : new FixtureChatClient(async (_, _, cancellationToken) =>
                {
                    Interlocked.Increment(ref started);
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }
                    finally
                    {
                        Interlocked.Increment(ref cancelled);
                    }
                    return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Unreachable."));
                });
            var result = await SampleHarness.RunAsync(sample, Prompts.MorningBrief, Factory,
                deadline: TimeSpan.FromMilliseconds(400));
            Check.Equal(3, started, "All three real child sessions started before deadline.");
            Check.Equal(started, cancelled, "Release must await cooperative cancellation of every child.");
            Check.True(!result.Completed && result.SessionReleased, "Deadline is not a successful brief.");
            Check.Equal(0, result.RunningAfterRelease, "Released provider must have no running tasks.");
            Check.True(result.Events.Any(entry => entry.State == "CANCELLED"), "Host cancellation event is visible.");
        }
    }

    public static async Task FailureAsync()
    {
        foreach (int sample in new[] { 42, 43 })
        {
            using var output = new StringWriter();
            IChatClient Factory(string name) => name == "MainAgent"
                ? SampleHarness.Script(sample, name, Prompts.News)
                : new FixtureChatClient((_, _, _) => throw new InvalidOperationException("FAKE-PRIVATE-ERROR-DETAIL"));
            bool failed = false;
            try
            {
                await SampleHarness.RunAsync(sample, Prompts.News, Factory, output: output);
            }
            catch (InvalidOperationException exception)
            {
                failed = true;
                Check.True(!exception.ToString().Contains("FAKE-PRIVATE-ERROR-DETAIL", StringComparison.Ordinal),
                    "Raw model error must not escape the worker guard.");
            }
            Check.True(failed, "A failed child must not become successful fixture synthesis.");
            Check.True(output.ToString().Contains("SESSION_RELEASED", StringComparison.Ordinal), "Failure must release the provider.");
            Check.True(!output.ToString().Contains("FAKE-PRIVATE-ERROR-DETAIL", StringComparison.Ordinal), "Raw cloud error text must not enter transcript.");
        }
    }
}
