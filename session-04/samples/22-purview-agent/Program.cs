// Objective: Sample 22 (MAF) makes the real Purview middleware visible before introducing any final app.
// A. Explain prerequisites without contacting a service.
// B. Wrap a live chat client with WithPurview using an approved local credential.
// C. Run a small generic assistant through that screened client.

using Azure.Identity;
using MafClaw.Samples;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Purview;
using Microsoft.Extensions.AI;

// A. A description is not an actual screening result.
if (args.SequenceEqual(["--describe"]))
{
    Console.WriteLine("PURVIEW PREREQUISITES: approved tenant/license, Graph permissions, Purview:Enabled=true and Purview:ClientId.");
    Console.WriteLine("No Purview request was made. Configure the approved service before running the demo.");
    return 2;
}

try
{
    if (args.Length != 0 && !args.SequenceEqual(["--live"]))
        throw new InvalidOperationException("Run with dotnet run. Offline preview: --describe.");
    var configuration = DemoSettings.ReadConfiguration();
    if (!bool.TryParse(configuration["Purview:Enabled"] ?? configuration["PURVIEW_ENABLED"], out var enabled) || !enabled ||
        !Guid.TryParse(configuration["Purview:ClientId"] ?? configuration["PURVIEW_CLIENT_APP_ID"], out var clientId))
    {
        Console.Error.WriteLine("Purview must be explicitly enabled with a valid approved application ID. There is no local fallback.");
        return 2;
    }

    // B. Microsoft.Agents.AI.Purview supplies organizational screening; this is not a homemade regex.
    var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions { ClientId = clientId.ToString() });
    using var screenedModel = DemoSettings.Load().CreateChatClient().AsBuilder()
        .WithPurview(credential, new PurviewSettings("WorkshopDemo")).Build();

    // C. MAF runs a generic assistant on the screened model. No tools or final-app capabilities are added.
    var agent = screenedModel.AsAIAgent(name: "ScreenedWorkshopAssistant",
        instructions: "Explain workshop topics briefly using only synthetic or public information.");
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
    DemoOutput.Print(await agent.RunAsync("Explain why human approval and data policy are different controls.",
        await agent.CreateSessionAsync(deadline.Token), cancellationToken: deadline.Token));
    Console.WriteLine("PURVIEW LIVE REQUEST COMPLETED. Verify the actual policy decision in the configured audit service.");
    return 0;
}
catch (Exception exception) { return DemoOutput.Report(exception); }
