using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var endpoint = config["Foundry:ProjectEndpoint"] ?? config["FOUNDRY_PROJECT_ENDPOINT"];
var model = config["Foundry:Model"] ?? config["FOUNDRY_MODEL"] ?? "gpt-5-mini";
var memoryStoreName = config["Foundry:MemoryStore"] ?? config["FOUNDRY_MEMORY_STORE"];
var embeddingModel = config["Foundry:EmbeddingModel"] ?? config["FOUNDRY_EMBEDDING_MODEL"];

if (string.IsNullOrWhiteSpace(endpoint))
{
    Console.WriteLine("Missing Foundry:ProjectEndpoint.");
    Console.WriteLine("Run from the repo root:");
    Console.WriteLine(@".\tools\configure-user-secrets.ps1 -Session 2");
    return;
}

var workingDirectory = Path.Combine(AppContext.BaseDirectory, "working");
Directory.CreateDirectory(workingDirectory);

var portfolioPath = Path.Combine(workingDirectory, "portfolio.csv");
if (!File.Exists(portfolioPath))
{
    File.WriteAllText(
        portfolioPath,
        "symbol,shares,averageCost,risk\nMSFT,35,430.12,moderate\nNVDA,20,142.50,high\nSPY,50,530.25,low\n");
}

var projectClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential());

FoundryMemoryProvider? foundryMemory = null;
if (!string.IsNullOrWhiteSpace(memoryStoreName) && !string.IsNullOrWhiteSpace(embeddingModel))
{
    foundryMemory = new FoundryMemoryProvider(
        projectClient,
        memoryStoreName,
        stateInitializer: _ => new(new FoundryMemoryProviderScope("mafclaw-session-02-user")),
        new FoundryMemoryProviderOptions
        {
            UpdateDelay = 0,
        });

    await foundryMemory.EnsureMemoryStoreCreatedAsync(
        model,
        embeddingModel,
        "Durable memory for the MafClaw Session 2 finance advisor.");

    Console.WriteLine($"Foundry memory enabled (store: {memoryStoreName}).");
}
else
{
    Console.WriteLine("Foundry memory disabled. Set Foundry:MemoryStore and Foundry:EmbeddingModel to enable durable profile recall.");
}

IChatClient chatClient = projectClient
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    FileAccessStore = new FileSystemAgentFileStore(workingDirectory),
    ToolApprovalAgentOptions = new ToolApprovalAgentOptions
    {
        AutoApprovalRules = [FileAccessProvider.ReadOnlyToolsAutoApprovalRule],
    },
    AIContextProviders = foundryMemory is null ? null : [foundryMemory],
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a personal finance education assistant for a live coding workshop.
            Use the provided Harness tools instead of inventing portfolio data.
            Explain that all values are mock educational data, not financial advice.

            Important safety boundaries:
            - The user's portfolio is in portfolio.csv inside the approved working folder.
            - Read portfolio data with the built-in file_access tools before answering portfolio questions.
            - Write reports with the built-in file_access tools under the approved working folder.
            - Read-only file operations are auto-approved; writes and destructive operations require Harness approval.
            - Remember durable user facts with the configured Foundry memory provider when it is enabled.
            - Simulated trades must go through request_simulated_trade, which is wrapped as an approval-required Harness tool.
            """,
        Tools =
        [
            AgentFinanceTools.RequestSimulatedTrade
        ]
    }
});

Console.WriteLine("mafclaw · Session 02");
Console.WriteLine("Agent Framework + Harness finance advisor");
Console.WriteLine("This app combines Harness file access, Harness approvals, and optional Foundry memory.");
Console.WriteLine();
Console.WriteLine("Try these prompts:");
Console.WriteLine("  What is in my portfolio?");
Console.WriteLine("  Write a short markdown report about my portfolio and save it.");
Console.WriteLine("  Remember that I am a conservative investor saving for a house in two years.");
Console.WriteLine("  What do you remember about my investor profile?");
Console.WriteLine("  Buy 10 shares of MSFT.");
Console.WriteLine();
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent, foundryMemory);
