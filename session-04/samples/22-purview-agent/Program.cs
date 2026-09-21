// Objective: exercise real Purview screening only with explicit prerequisites.
// A. Describe the licensing/configuration gate without pretending to screen.
// B. Require opt-in settings for the published WithPurview integration.
// C. Make one bounded live request; never substitute a local rule.
using MafClaw.Session04;

if (args.Length == 0 || args.SequenceEqual(["--describe"]))
{
    Console.WriteLine("PURVIEW PREREQUISITES: approved tenant/license, Graph permissions, Purview:Enabled=true and Purview:ClientId.");
    Console.WriteLine("No Purview request was made. Use --live only after configuration and authorization.");
    return 2;
}
try
{
    if (!args.SequenceEqual(["--live"])) throw new FinanceConfigurationException("Usage: --describe | --live");
    var settings = FinanceSettings.Load();
    if (!settings.PurviewEnabled) throw new FinanceConfigurationException("Purview must be explicitly enabled; there is no fallback.");
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Settings = settings, EnableShell = false, EnableCodeAct = false, EnableMemory = false, EnableResearch = false
    }, deadline.Token);
    var response = await build.Agent.RunAsync("Explain diversification using only mock educational data.",
        await build.CreateSessionAsync(deadline.Token), cancellationToken: deadline.Token);
    FinanceConsole.PrintResponse(response);
    Console.WriteLine("PURVIEW LIVE REQUEST COMPLETED. Verify the actual policy decision in the configured audit service.");
    return 0;
}
catch (Exception exception) { return SafeErrors.Report(exception); }
