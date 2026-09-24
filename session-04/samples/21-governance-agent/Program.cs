// Objective: Sample 21 (MAF/Harness) pauses a mock publish action for an explicit human decision.
// A. Define one in-memory tool and wrap it in ApprovalRequiredAIFunction.
// B. Build the Harness here and inspect its pending request before any side effect.
// C. Return approval/denial to the same session and verify the actual outbox.

using System.ComponentModel;
using System.Text.Json;
using MafClaw.Samples;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

try
{
    if (args.Length > 0 && (args.Length > 2 || args[0] is not ("--fixture" or "--live") ||
        (args.Length == 2 && (args[0] != "--fixture" || args[1] != "--approve"))))
        throw new InvalidOperationException("Run with dotnet run. Automated checks: --fixture [--approve].");
    var fixture = args.Contains("--fixture");
    var outbox = new List<string>();

    // A. Only the framework wrapper adds approval. The underlying function is ordinary C#.
    [Description("Add a synthetic note to an in-memory outbox. No external message is sent.")]
    string PublishNote(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        outbox.Add(text);
        return $"MOCK PUBLISHED: {text}";
    }
    var tool = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(PublishNote, "publish_note"));
    using IChatClient model = fixture
        ? new FixtureChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new FunctionCallContent("publish-demo", "publish_note",
                    new Dictionary<string, object?> { ["text"] = "Welcome to the workshop." })])),
            FixtureChatClient.Text("[FIXTURE] The approval decision was processed."))
        : DemoSettings.Load().CreateChatClient();

    // B. Microsoft.Agents.AI.Harness owns the pause/resume protocol, not a custom pending-call queue.
    var agent = model.AsHarnessAgent(new HarnessAgentOptions
    {
        Name = "ApprovalDemo",
        DisableFileMemory = true, DisableAgentSkillsProvider = true, DisableWebSearch = true,
        DisableTodoProvider = true, DisableAgentModeProvider = true, DisableOpenTelemetry = true,
        DisableToolAutoApproval = true,
        ChatOptions = new ChatOptions
        {
            Tools = [tool],

            // Keep this tool lesson short; this deployment requires reasoning off for Chat Completions tools.
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None },
            Instructions = "Use publish_note to propose the requested note. The host owns approval."
        }
    });
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
    var session = await agent.CreateSessionAsync(deadline.Token);
    var response = await agent.RunAsync("Publish the synthetic note: Welcome to the workshop.",
        session, options: new ChatClientAgentRunOptions(new ChatOptions
        {
            ToolMode = ChatToolMode.RequireSpecific("publish_note"), AllowMultipleToolCalls = false
        }), cancellationToken: deadline.Token);
    var requests = response.Messages.SelectMany(message => message.Contents).OfType<ToolApprovalRequestContent>().ToArray();
    if (requests.Length != 1 || outbox.Count != 0)
        throw new InvalidOperationException("Expected one approval request and no side effect.");
    Console.WriteLine("BEFORE DECISION: outbox=0");
    Console.WriteLine($"Proposed: {JsonSerializer.Serialize(((FunctionCallContent)requests[0].ToolCall).Arguments)}");

    // C. The fixture denies by default; --approve is a deterministic, local-only comparison.
    Console.WriteLine(fixture ? "FIXTURE decision selected locally." : "Approve this exact mock note? [y/N]");
    var approved = fixture ? args.Contains("--approve")
        : string.Equals(await Console.In.ReadLineAsync(deadline.Token), "y", StringComparison.OrdinalIgnoreCase);
    response = await agent.RunAsync([new ChatMessage(ChatRole.User, [requests[0].CreateResponse(approved)])],
        session, cancellationToken: deadline.Token);
    DemoOutput.Print(response);
    Console.WriteLine($"AFTER DECISION: outbox={outbox.Count}; approved={approved}. No message was sent.");
    var passed = outbox.Count == (approved ? 1 : 0);
    Console.WriteLine(passed ? "GOVERNANCE AGENT PASS" : "GOVERNANCE AGENT FAIL");
    return passed ? 0 : 1;
}
catch (Exception exception) { return DemoOutput.Report(exception); }
