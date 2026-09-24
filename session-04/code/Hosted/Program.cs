// Objective: expose the complete Session 4 agent's restricted hosted profile over Responses HTTP.
// A. Choose explicit fixture inference or validated non-interactive live identity.
// B. Apply the restricted hosted profile.
// C. Let the MAF hosting package own Responses routing and its telemetry pipeline.

using Azure.Identity;
using MafClaw.Session04;
using MafClaw.Session04.Hosting;

try
{
    // A. Hosted live uses managed identity, not the presenter's interactive Azure CLI login.
    // Supplying a scripted client keeps the explicit fixture path independent of model credentials.
    var fixture = args.Contains("--fixture");
    var settings = fixture ? null : FinanceSettings.Load();

    // B. The factory removes local capabilities; a different host does not imply the same authority.
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Profile = FinanceHostProfile.Hosted,
        Settings = settings,
        ChatClient = fixture ? ScriptedChatClient.Portfolio() : null,
        Credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned),
        EnableResearch = !fixture
    });

    // C. FinanceWebHost registers Microsoft.Agents.AI.Foundry.Hosting's Responses endpoints.
    // The SDK handles the wire protocol; this entry point selects identity/profile and starts ASP.NET.
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
