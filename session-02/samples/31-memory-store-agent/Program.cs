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

var projectClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential());

FoundryMemoryProvider? foundryMemory = null;
if (!string.IsNullOrWhiteSpace(memoryStoreName) && !string.IsNullOrWhiteSpace(embeddingModel))
{
    foundryMemory = new FoundryMemoryProvider(
        projectClient,
        memoryStoreName,
        stateInitializer: _ => new(new FoundryMemoryProviderScope("mafclaw-session-02-sample-user")),
        new FoundryMemoryProviderOptions
        {
            UpdateDelay = 0,
        });

    await foundryMemory.EnsureMemoryStoreCreatedAsync(
        model,
        embeddingModel,
        "Durable memory for the MafClaw Session 2 memory sample.");

    Console.WriteLine($"Foundry memory enabled (store: {memoryStoreName}).");
}
else
{
    Console.WriteLine("Foundry memory disabled. Set Foundry:MemoryStore and Foundry:EmbeddingModel to enable durable memory.");
}

IChatClient chatClient = projectClient
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    AIContextProviders = foundryMemory is null ? null : [foundryMemory],
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a finance education assistant.
            Remember durable facts the user tells you about their investing profile, goals, and preferences.
            When memory is enabled, Microsoft Foundry extracts and recalls those facts through the configured memory provider.
            Explain whether Foundry memory is enabled before relying on cross-session recall.
            """,
    }
});

Console.WriteLine("mafclaw · Session 02 sample 31");
Console.WriteLine("Agentic memory with Microsoft Agent Framework + Harness + FoundryMemoryProvider.");
Console.WriteLine("Try: Remember that I am a conservative investor.");
Console.WriteLine("Then start a new session and ask: What do you remember about my investor profile?");
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent, foundryMemory);
