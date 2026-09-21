// Objective: observe the actual MAF agent/tool loop, not just a local calculation.
// A. Explicitly select scripted or live inference.
// B. Export MAF spans through the shared privacy-filtered OpenTelemetry setup.
// C. Display actual tool results and verify that a tool ran.
using MafClaw.Session04;
using Microsoft.Extensions.AI;

try
{
    if (args.Length != 1 || args[0] is not ("--fixture" or "--live"))
        throw new FinanceConfigurationException("Usage: --fixture | --live");
    var fixture = args[0] == "--fixture";
    var settings = fixture ? null : FinanceSettings.Load();
    using var telemetry = new FinanceTelemetry(settings?.OtlpEndpoint ?? FinanceSettings.LoadTelemetryEndpoint(), console: true);
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Profile = fixture ? FinanceHostProfile.Fixture : FinanceHostProfile.Local,
        Settings = settings, ChatClient = fixture ? ScriptedChatClient.Portfolio() : null,
        EnableShell = false, EnableCodeAct = false, EnableMemory = false, EnableResearch = false
    });
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    Console.WriteLine(fixture ? "FIXTURE inference; real MAF and OpenTelemetry." : "LIVE inference; real MAF and OpenTelemetry.");
    var response = await build.Agent.RunAsync(FinanceEvaluations.Query,
        await build.CreateSessionAsync(deadline.Token), cancellationToken: deadline.Token);
    FinanceConsole.PrintResponse(response);
    var passed = response.Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Any();
    Console.WriteLine(passed ? "OBSERVABILITY AGENT PASS" : "OBSERVABILITY AGENT FAIL: no tool evidence");
    return passed ? 0 : 1;
}
catch (Exception exception) { return SafeErrors.Report(exception); }
