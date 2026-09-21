// Objective: run the cumulative agent interactively or demonstrate an explicit fixture.
// A. Select live/fixture behavior and optional local capabilities.
// B. Build the shared agent and configure non-sensitive telemetry.
// C. Run the console, then release sessions and providers.
using MafClaw.Session04;

try
{
    var fixture = args.Contains("--fixture");
    var allowed = new[] { "--fixture", "--no-shell", "--no-codeact", "--trace" };
    if (args.Any(argument => !allowed.Contains(argument)))
        throw new FinanceConfigurationException("Usage: [--fixture] [--no-shell] [--no-codeact] [--trace]");
    var settings = fixture ? null : FinanceSettings.Load();
    using var telemetry = new FinanceTelemetry(settings?.OtlpEndpoint ?? FinanceSettings.LoadTelemetryEndpoint(),
        console: args.Contains("--trace"));
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Profile = fixture ? FinanceHostProfile.Fixture : FinanceHostProfile.Local,
        Settings = settings, ChatClient = fixture ? ScriptedChatClient.Portfolio() : null,
        EnableShell = !args.Contains("--no-shell"), EnableCodeAct = !args.Contains("--no-codeact"),
        EnableResearch = !fixture
    });
    Console.WriteLine(fixture ? "FIXTURE: scripted inference, actual MAF tool execution. Ask for the portfolio once."
        : "LIVE: cumulative MAF finance advisor. CodeAct requires approval; shell is not an OS sandbox.");
    await FinanceConsole.RunAsync(build);
    return 0;
}
catch (Exception exception)
{
    return SafeErrors.Report(exception);
}
