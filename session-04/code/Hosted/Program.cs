// Objective: serve the shared agent over the Foundry Responses protocol.
// A. Choose explicit fixture inference or validated non-interactive live identity.
// B. Apply the restricted hosted profile.
// C. Let the MAF hosting package own Responses routing and its telemetry pipeline.
using Azure.Identity;
using MafClaw.Session04;
using MafClaw.Session04.Hosting;

try
{
    var fixture = args.Contains("--fixture");
    var settings = fixture ? null : FinanceSettings.Load();
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Profile = FinanceHostProfile.Hosted,
        Settings = settings,
        ChatClient = fixture ? ScriptedChatClient.Portfolio() : null,
        Credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned),
        EnableResearch = !fixture
    });
    var app = FinanceWebHost.Create(args.Where(argument => argument != "--fixture").ToArray(), build, fixture);
    Console.WriteLine(fixture ? "HOSTED FIXTURE: scripted inference; not a deployed cloud agent."
        : "HOSTED LIVE: managed identity; local file/shell/memory/CodeAct/trade capabilities disabled.");
    await app.RunAsync();
    return 0;
}
catch (Exception exception)
{
    return SafeErrors.Report(exception);
}
