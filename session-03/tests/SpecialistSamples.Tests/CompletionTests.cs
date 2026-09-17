// Objective: reject narrative-only success while preserving foreground evidence.
// A. Challenge required scenarios with no dispatch and wrong role subsets.
// B. Use actual SDK terminal failures and uncollected successful tasks.
// C. Verify narrow worker exception handling and nonzero completion exit codes.

using System.Globalization;
using System.Text.RegularExpressions;
using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;
using Prompts = MafClaw.Sample43.ScenarioPrompts;

namespace MafClaw.SpecialistSamples.Tests;

public static class CompletionTests
{
    private const string FalseClaim = "All specialists completed successfully. This is an intentionally false scripted claim.";

    public static async Task FalseClaimsAsync()
    {
        foreach (int sample in new[] { 42, 43 })
        {
            foreach (string prompt in new[] { Prompts.MorningBrief, Prompts.News, Prompts.Portfolio })
            {
                using var liar = new FixtureChatClient((_, _, _) =>
                    Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, FalseClaim))));
                ObservedTurn result = await SampleHarness.RunAsync(sample, prompt, live: liar);
                AssertIncomplete(result);
                Check.Equal(FalseClaim, result.Narrative, "False narrative remains visible as evidence.");
                Check.True(!result.Events.Any(entry => entry.State == "TOOL_CALL"), "No dispatched tool evidence was invented.");
            }
            ObservedTurn wrongSubset = await SampleHarness.RunAsync(sample, Prompts.MorningBrief,
                clients: name => SampleHarness.Script(sample, name, Prompts.News));
            AssertIncomplete(wrongSubset);
            Check.True(wrongSubset.Events.Any(entry => entry.State == "TASK_STATUS" && entry.Detail.Contains("NewsAgent=Completed", StringComparison.Ordinal)),
                "One successful news worker is not a full morning brief.");
        }
    }

    public static async Task TerminalOutcomesAsync()
    {
        foreach (int sample in new[] { 42, 43 })
        {
            foreach (bool failWorker in new[] { false, true })
            {
                int stage = 0, taskId = 0;
                IChatClient Factory(string name)
                {
                    if (name != "MainAgent")
                    {
                        return failWorker
                            ? new FixtureChatClient((_, _, _) => throw new IOException("SYNTHETIC-PRIVATE-WORKER-DETAIL"))
                            : SampleHarness.Script(sample, name, Prompts.News);
                    }
                    return new FixtureChatClient((messages, _, _) =>
                    {
                        ChatResponse response;
                        if (stage == 0)
                        {
                            response = Call("start", "background_agents_start_task", new()
                            {
                                ["agentName"] = "NewsAgent", ["input"] = "Return educational news.", ["description"] = "News"
                            });
                        }
                        else if (stage == 1)
                        {
                            string actualStart = messages.SelectMany(message => message.Contents)
                                .OfType<FunctionResultContent>().Single(result => result.CallId == "start").Result!.ToString()!;
                            Match match = Regex.Match(actualStart, @"^Background task (\d+) started");
                            Check.True(match.Success, "Read the actual SDK-generated task ID.");
                            taskId = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                            response = Call("wait", "background_agents_wait_for_first_completion",
                                new() { ["taskIds"] = new[] { taskId } });
                        }
                        else if (stage == 2 && failWorker)
                        {
                            response = Call("collect", "background_agents_get_task_results", new() { ["taskId"] = taskId });
                        }
                        else
                        {
                            response = new ChatResponse(new ChatMessage(ChatRole.Assistant, FalseClaim));
                        }
                        stage++;
                        return Task.FromResult(response);
                    });
                }
                ObservedTurn result = await SampleHarness.RunAsync(sample, Prompts.News, clients: Factory);
                AssertIncomplete(result);
                Check.Equal(FalseClaim, result.Narrative, "Terminal failure must not erase foreground narrative evidence.");
                Check.True(result.Events.Any(entry => entry.State == "TASK_STATUS" &&
                    entry.Detail.Contains(failWorker ? "NewsAgent=Failed" : "NewsAgent=Completed", StringComparison.Ordinal)),
                    "Verify the actual SDK terminal outcome, not GetIncompleteTasks.");
                Check.True(!result.Events.Any(entry => entry.Detail.Contains("SYNTHETIC-PRIVATE-WORKER-DETAIL", StringComparison.Ordinal)),
                    "Expected worker failure details remain sanitized.");
            }
        }
    }

    public static async Task ExceptionFilterAsync()
    {
        foreach (int sample in new[] { 42, 43 })
        {
            foreach (Exception original in new Exception[]
            {
                new IOException("Synthetic expected IO failure"),
                new ApplicationException("Synthetic unexpected defect"),
                new OutOfMemoryException("Synthetic fatal exception; no memory pressure was generated"),
                new OperationCanceledException("Synthetic cancellation")
            })
            {
                using var client = new FixtureChatClient((_, _, _) => throw original);
                using IChatClient guard = sample == 42
                    ? new MafClaw.Sample42.WorkerFailureGuard(client)
                    : new MafClaw.Sample43.WorkerFailureGuard(client);
                Exception? caught = null;
                try
                {
                    await guard.GetResponseAsync([new ChatMessage(ChatRole.User, "Offline guard probe.")]);
                }
                catch (Exception exception)
                {
                    caught = exception;
                }
                if (original is IOException)
                {
                    Check.True(caught is InvalidOperationException && caught.InnerException is null &&
                        !caught.Message.Contains(original.Message, StringComparison.Ordinal), "Expected IO failure must be sanitized.");
                }
                else
                {
                    Check.True(ReferenceEquals(original, caught), "Unexpected, fatal and cancellation exceptions must propagate unchanged.");
                }
            }
        }
    }

    private static ChatResponse Call(string id, string name, Dictionary<string, object?> arguments) =>
        new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(id, name, arguments)]));

    private static void AssertIncomplete(ObservedTurn result)
    {
        Check.True(result.ForegroundFinished, "The foreground turn finished independently of specialist verification.");
        Check.True(!result.Completed && result.ExitCode != 0, "Unverified specialist work must remain incomplete/nonzero.");
        Check.True(result.SessionReleased && result.RunningAfterRelease == 0, "Incomplete work still releases all sessions.");
        Check.True(result.Events.Any(entry => entry.State == "UNVERIFIED_RESULTS"), "The missing outcome must be explicit.");
    }
}
