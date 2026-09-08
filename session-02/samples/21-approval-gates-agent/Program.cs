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
            The request_simulated_trade tool is wrapped with the Harness approval-required function wrapper.
            Explain that Harness approvals protect users before side effects happen.
            Never claim that a real trade was placed.
            """,
        Tools =
        [
            ApprovalGateTools.RequestSimulatedTrade
        ]
    }
});

Console.WriteLine("mafclaw · Session 02 sample 21");
Console.WriteLine("Agentic approval gate with Microsoft Agent Framework + Harness.");
Console.WriteLine("Try: Buy 10 shares of MSFT.");
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent);
