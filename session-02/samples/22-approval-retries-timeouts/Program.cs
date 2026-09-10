// Session flow:
// A. Load Foundry chat settings.
// B. Create a Harness agent with one approval-required trade tool.
// C. Apply a five-attempt, five-second approval policy.
// D. Demonstrate approve, deny, retry, timeout, and automatic denial.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var endpoint = config["Foundry:ProjectEndpoint"] ?? config["FOUNDRY_PROJECT_ENDPOINT"];
var model = config["Foundry:Model"] ?? config["FOUNDRY_MODEL"] ?? "gpt-5-mini";

if (string.IsNullOrWhiteSpace(endpoint))
{
    Console.WriteLine("Missing Foundry:ProjectEndpoint.");
    Console.WriteLine("Run from the repo root:");
    Console.WriteLine(@".\tools\configure-user-secrets.ps1 -Session 2");
    return;
}

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a finance education assistant.
            Any simulated trade must use request_simulated_trade.
            The trade tool requires Harness approval before execution.
            If approval is denied or expires, clearly explain that no simulated trade was executed.
            Never claim that a real trade was placed.
            """,
        Tools =
        [
            ApprovalGateTools.RequestSimulatedTrade
        ]
    }
});

const int maxApprovalAttempts = 5;
var approvalTimeout = TimeSpan.FromSeconds(5);
var approvalPolicy = new TimedApprovalPolicy(maxApprovalAttempts, approvalTimeout);

Console.WriteLine("mafclaw · Session 02 sample 22");
Console.WriteLine("Approval retries and timeouts with Microsoft Agent Framework + Harness.");
Console.WriteLine("Approved path: Buy 10 shares of MSFT. Answer y before the timeout.");
Console.WriteLine("Denied path  : Buy 10 shares of MSFT. Answer n.");
Console.WriteLine("Retry path   : Enter an invalid value, then answer y or n.");
Console.WriteLine("Timeout path : Do not answer; after five 5-second attempts the request is denied.");
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent, approvalPolicy);
