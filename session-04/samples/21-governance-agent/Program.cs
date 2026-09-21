// Objective: bind a model-proposed side effect to an actual MAF approval decision.
// A. Select scripted or live inference.
// B. Show the pending request before any simulated trade executes.
// C. Deny in the fixture, or accept the interactive user's explicit decision.
using System.Text.Json;
using MafClaw.Session04;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

try
{
    if (args.Length != 1 || args[0] is not ("--fixture" or "--live"))
        throw new FinanceConfigurationException("Usage: --fixture | --live");
    var fixture = args[0] == "--fixture";
    var client = fixture ? new ScriptedChatClient(
        new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent("fixture-trade", "request_simulated_trade",
                new Dictionary<string, object?> { ["side"] = "buy", ["symbol"] = "MSFT", ["shares"] = 2 })])),
        ScriptedChatClient.Text("The decision has been processed.")) : null;
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Profile = fixture ? FinanceHostProfile.Fixture : FinanceHostProfile.Local,
        Settings = fixture ? null : FinanceSettings.Load(), ChatClient = client,
        EnableShell = false, EnableCodeAct = false, EnableMemory = false, EnableResearch = false
    });
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    var session = await build.CreateSessionAsync(deadline.Token);
    var response = await build.Agent.RunAsync("Simulate buying 2 shares of MSFT using request_simulated_trade.",
        session, options: new ChatClientAgentRunOptions(new ChatOptions
        {
            ToolMode = ChatToolMode.RequireSpecific("request_simulated_trade"),
            AllowMultipleToolCalls = false
        }), cancellationToken: deadline.Token);
    var requests = response.Messages.SelectMany(message => message.Contents).OfType<ToolApprovalRequestContent>().ToArray();
    if (requests.Length != 1 || build.Tools.ExecutedTrades.Count != 0)
        throw new InvalidOperationException("Expected one pending approval and no side effect.");
    var call = (FunctionCallContent)requests[0].ToolCall;
    Console.WriteLine($"Proposed: {call.Name} {JsonSerializer.Serialize(call.Arguments)}");
    Console.WriteLine(fixture ? "FIXTURE decision: deny." : "Approve this exact simulated operation? [y/N]");
    var approved = !fixture && string.Equals(await Console.In.ReadLineAsync(deadline.Token), "y", StringComparison.OrdinalIgnoreCase);
    response = await build.Agent.RunAsync([new ChatMessage(ChatRole.User, [requests[0].CreateResponse(approved)])],
        session, cancellationToken: deadline.Token);
    FinanceConsole.PrintResponse(response);
    var passed = build.Tools.ExecutedTrades.Count == (approved ? 1 : 0);
    Console.WriteLine($"Actual simulated trades: {build.Tools.ExecutedTrades.Count}. No real transactions.");
    Console.WriteLine(passed ? "GOVERNANCE AGENT PASS" : "GOVERNANCE AGENT FAIL");
    return passed ? 0 : 1;
}
catch (Exception exception) { return SafeErrors.Report(exception); }
