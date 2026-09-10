// Session flow:
// A. Load the Foundry connection settings.
// B. Build a Harness agent with one trade tool.
// C. Let the model request one simulated trade.
// D. Run it twice: approve once, then deny once.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

// Read configuration from .NET user-secrets first, then environment variables.
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// The agentic sample needs a Foundry project and model to run live.
var endpoint = config["Foundry:ProjectEndpoint"] ?? config["FOUNDRY_PROJECT_ENDPOINT"];
var model = config["Foundry:Model"] ?? config["FOUNDRY_MODEL"] ?? "gpt-5-mini";

// Stop early with setup guidance when the sample is not configured yet.
if (string.IsNullOrWhiteSpace(endpoint))
{
    Console.WriteLine("Missing Foundry:ProjectEndpoint.");
    Console.WriteLine("Run from the repo root:");
    Console.WriteLine(@".\tools\configure-user-secrets.ps1 -Session 2");
    return;
}

// Adapt the Foundry project client into the chat client shape expected by Harness.
IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

// Give the agent only the approval-gated trade tool.
AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a finance education assistant.
            Any simulated trade must use request_simulated_trade.
            The request_simulated_trade tool is wrapped with the Harness approval-required function wrapper.
            Explain that Harness approvals protect users before side effects happen.
            If the user denies approval, explain that no simulated trade was executed.
            Never claim that a real trade was placed.
            """,
        Tools =
        [
            ApprovalGateTools.RequestSimulatedTrade
        ]
    }
});

// Keep the live demo simple: one prompt loop, one clear exit command.
Console.WriteLine("mafclaw · Session 02 sample 21");
Console.WriteLine("Agentic approval gate with Microsoft Agent Framework + Harness.");
Console.WriteLine("Approved path: Buy 10 shares of MSFT. Then answer y at the approval prompt.");
Console.WriteLine("Denied path  : Buy 10 shares of MSFT. Then answer n at the approval prompt.");
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent);
