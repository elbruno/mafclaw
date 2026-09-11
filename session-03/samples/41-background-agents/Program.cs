// Session flow:
// A. Load the Foundry connection settings.
// B. Build a lean research sub-agent with only a web-search tool.
// C. Hand it to the main Harness agent as a BackgroundAgent so work can run concurrently.
// D. Ask the agent to research multiple tickers and aggregate the findings.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var endpoint = configuration["Foundry:ProjectEndpoint"] ?? configuration["FOUNDRY_PROJECT_ENDPOINT"];
var model = configuration["Foundry:Model"] ?? configuration["FOUNDRY_MODEL"] ?? "gpt-5-mini";

if (string.IsNullOrWhiteSpace(endpoint))
{
    Console.WriteLine("Missing Foundry:ProjectEndpoint.");
    Console.WriteLine("Run from the repository root:");
    Console.WriteLine(@".\tools\configure-user-secrets.ps1 -Session 3");
    return;
}

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

// A lean, web-search-only sub-agent. No Harness machinery: it only needs to research one ticker.
AIAgent research = chatClient.AsAIAgent(
    name: "TickerResearchAgent",
    description: "Searches the web for recent news about a single stock ticker.",
    instructions: "You research a single ticker and return 3-4 factual bullet points with sources.",
    tools: [new HostedWebSearchTool()]);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    BackgroundAgents = [research],
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a finance-education assistant. When asked to research multiple tickers,
            fan the work out using the background_agents_* tools so each ticker is researched
            concurrently, then collect and summarize the findings together.
            State clearly that this is educational research, not investment advice.
            """,
    },
});

Console.WriteLine("Sample 41 - Microsoft Agent Framework background agents");
Console.WriteLine("The Harness exposes background_agents_* tools to fan work out to sub-agents that run concurrently.");
Console.WriteLine("Try: Research MSFT, NVDA and SPY and summarize the latest news.");
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent);
