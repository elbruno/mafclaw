// Objective: verify real tool evidence, fixture behavior, and safe error reporting offline.
// Steps:
// A. Substitute an explicitly scripted IChatClient, never a cloud endpoint.
// B. Exercise MAF invocation and concurrent transcript behavior.
// C. Report each check and propagate failures through the process exit code.

using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.OrchestrationSupport.Tests;

internal static class SupportTests
{
    public static async Task<int> RunAsync()
    {
        (string Name, Func<Task> Run)[] tests =
        [
            ("actual MAF function calls/results are traced but instructions are not", async () =>
            {
                using var output = new StringWriter();
                var transcript = new Transcript(output);
                var executions = 0;
                using var fixture = new FixtureChatClient((messages, _, _) =>
                    Task.FromResult(messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Any()
                        ? new ChatResponse(new ChatMessage(ChatRole.Assistant, "Scripted summary."))
                        : new ChatResponse(new ChatMessage(ChatRole.Assistant,
                            [new FunctionCallContent("fixture-call", "calculate", new Dictionary<string, object?>())]))));
                var client = new TracingChatClient(fixture, "Calculator", transcript);
                var agent = client.AsAIAgent(
                    name: "Calculator",
                    instructions: "DO-NOT-LOG-INSTRUCTION-BODY",
                    tools: [AIFunctionFactory.Create(() => { executions++; return 42; }, "calculate")]);
                var session = await agent.CreateSessionAsync();
                var response = await agent.RunAsync("DO-NOT-LOG-USER-BODY", session);
                Check(executions == 1 && response.Text == "Scripted summary.", "MAF did not invoke the real function exactly once.");
                Check(transcript.Entries.Any(entry => entry.State == "TOOL_CALL" && entry.Detail.Contains("calculate")), "Missing actual tool request.");
                Check(transcript.Entries.Any(entry => entry.State == "TOOL_RESULT" && entry.Detail.Contains("42")), "Missing actual tool result.");
                Check(!output.ToString().Contains("DO-NOT-LOG"), "Instruction or user body leaked into the tool transcript.");
            }),
            ("reused fixture call IDs in different content objects remain visible", async () =>
            {
                var transcript = new Transcript(TextWriter.Null);
                using var fixture = new FixtureChatClient((_, _, _) => Task.FromResult(new ChatResponse(
                    new ChatMessage(ChatRole.Assistant,
                        [new FunctionCallContent("same-id", "read_mock_data", new Dictionary<string, object?>())]))));
                var client = new TracingChatClient(fixture, "Worker", transcript);
                await client.GetResponseAsync([new ChatMessage(ChatRole.User, "First job.")]);
                await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Different job.")]);
                Check(transcript.Entries.Count == 2, "A different job was hidden by call-ID deduplication.");
            }),
            ("historical result objects are not repeatedly reported as new results", async () =>
            {
                var transcript = new Transcript(TextWriter.Null);
                using var fixture = new FixtureChatClient((_, _, _) =>
                    Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Fixture."))));
                var client = new TracingChatClient(fixture, "Worker", transcript);
                ChatMessage[] history = [new(ChatRole.Tool, [new FunctionResultContent("id", "observed")])];
                await client.GetResponseAsync(history);
                await client.GetResponseAsync(history);
                Check(transcript.Entries.Count == 1, "The same historical result was duplicated.");
            }),
            ("fixture cancellation does not run inference script", async () =>
            {
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                using var client = new FixtureChatClient((_, _, _) =>
                    throw new InvalidOperationException("Script must not run."));
                try
                {
                    await client.GetResponseAsync([], cancellationToken: cancellation.Token);
                    throw new InvalidOperationException("Cancellation was ignored.");
                }
                catch (OperationCanceledException)
                {
                    Check(client.Calls == 0, "A cancelled request reached the script.");
                }
            }),
            ("concurrent transcript lines and retained evidence are bounded", () =>
            {
                using var output = new StringWriter();
                var transcript = new Transcript(output);
                Parallel.For(0, 2000, number => transcript.Write($"Worker-{number}", "OBSERVED", number.ToString()));
                Check(transcript.Entries.Count == 1000, "Retained transcript is unbounded.");
                Check(output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length == 2000,
                    "Concurrent lines were lost or interleaved.");
                transcript.Write("Worker", "RESULT", new string('x', 7000));
                Check(transcript.Entries[^1].Detail.EndsWith("[transcript excerpt truncated]"), "Truncation was silent.");
                return Task.CompletedTask;
            }),
            ("transport errors expose no raw service message", async () =>
            {
                using var error = new StringWriter();
                var original = Console.Error;
                try
                {
                    Console.SetError(error);
                    var exitCode = await SafeConsole.RunAsync(() =>
                        Task.FromException<int>(new HttpRequestException("PRIVATE-TRANSPORT-DETAIL")));
                    Check(exitCode == 1 && error.ToString().Contains("connection failed"), "Failure was not explicit.");
                    Check(!error.ToString().Contains("PRIVATE-TRANSPORT-DETAIL"), "Raw transport detail was printed.");
                }
                finally
                {
                    Console.SetError(original);
                }
            })
        ];

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Run();
                Console.WriteLine($"PASS: {test.Name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL: {test.Name}: {exception.Message}");
            }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} orchestration-support checks passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException(message); }
    }
}
