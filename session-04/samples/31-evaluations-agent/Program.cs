// Objective: use MAF's LocalEvaluator and EvaluateAsync on actual tool execution.
// A. Select explicit fixture or live inference.
// B. Build the same agent and grade exact values plus tool evidence.
// C. Return a failing exit code when any required check fails.
using MafClaw.Session04;
using Microsoft.Agents.AI;

try
{
    if (args.Length != 1 || args[0] is not ("--fixture" or "--live"))
        throw new FinanceConfigurationException("Usage: --fixture | --live");
    var fixture = args[0] == "--fixture";
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Profile = fixture ? FinanceHostProfile.Fixture : FinanceHostProfile.Local,
        Settings = fixture ? null : FinanceSettings.Load(),
        ChatClient = fixture ? ScriptedChatClient.Portfolio() : null,
        EnableShell = false, EnableCodeAct = false, EnableMemory = false, EnableResearch = false
    }, deadline.Token);
    // Microsoft.Agents.AI supplies evaluation execution and result aggregation.
    var results = await build.Agent.EvaluateAsync([FinanceEvaluations.Query],
        FinanceEvaluations.CreateLocalEvaluator(), cancellationToken: deadline.Token);
    Console.WriteLine($"Inference: {(fixture ? "scripted fixture" : "LIVE, chargeable")}; grading: local MAF.");
    Console.WriteLine($"{results.Passed}/{results.Total} agent cases passed.");
    Console.WriteLine(results.AllPassed ? "EVALUATION AGENT PASS" : "EVALUATION AGENT FAIL");
    return results.AllPassed ? 0 : 1;
}
catch (Exception exception) { return SafeErrors.Report(exception); }
