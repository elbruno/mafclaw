// Session flow:
// A. Load the Foundry and memory settings.
// B. Create the optional platform-backed memory provider.
// C. Attach memory to the Harness agent context.
// D. Ask one user-memory question and one cross-user memory question.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

// Read configuration from .NET user-secrets first, then environment variables.
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// Memory needs both a logical memory store and an embedding model.
var endpoint = config["Foundry:ProjectEndpoint"] ?? config["FOUNDRY_PROJECT_ENDPOINT"];
var model = config["Foundry:Model"] ?? config["FOUNDRY_MODEL"] ?? "gpt-5-mini";
var memoryStoreName = config["Foundry:MemoryStore"] ?? config["FOUNDRY_MEMORY_STORE"];
var embeddingModel = config["Foundry:EmbeddingModel"] ?? config["FOUNDRY_EMBEDDING_MODEL"];
var memoryScope = config["Foundry:MemoryScope"] ?? config["FOUNDRY_MEMORY_SCOPE"] ?? "mafclaw-session-02-sample-user";

// Stop early with setup guidance when the sample is not configured yet.
if (string.IsNullOrWhiteSpace(endpoint))
{
    Console.WriteLine("Missing Foundry:ProjectEndpoint.");
    Console.WriteLine("Run from the repo root:");
    Console.WriteLine(@".\tools\configure-user-secrets.ps1 -Session 2");
    return;
}

// The project client is reused for chat and memory setup.
var projectClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential());

FoundryMemoryProvider? foundryMemory = null;
FoundryMemoryDemoStore? demoMemoryStore = null;
if (!string.IsNullOrWhiteSpace(memoryStoreName) && !string.IsNullOrWhiteSpace(embeddingModel))
{
    // Scope memory to this workshop sample user so recalls stay predictable.
    foundryMemory = new FoundryMemoryProvider(
        projectClient,
        memoryStoreName,
        stateInitializer: _ => new(new FoundryMemoryProviderScope(memoryScope)),
        new FoundryMemoryProviderOptions
        {
            UpdateDelay = 0,
            StorageInputRequestMessageFilter = _ => Enumerable.Empty<ChatMessage>(),
            StorageInputResponseMessageFilter = _ => Enumerable.Empty<ChatMessage>(),
        });
    demoMemoryStore = new FoundryMemoryDemoStore(projectClient, memoryStoreName, memoryScope);

    // Create or reuse the service-side memory store before starting the agent.
    await foundryMemory.EnsureMemoryStoreCreatedAsync(
        model,
        embeddingModel,
        "Durable memory for the MafClaw Session 2 memory sample.");

    Console.WriteLine($"Foundry memory enabled (store: {memoryStoreName}, scope: {memoryScope}).");
}
else
{
    Console.WriteLine("Foundry memory disabled. Set Foundry:MemoryStore and Foundry:EmbeddingModel to enable durable memory.");
}

// Adapt the Foundry project client into the chat client shape expected by Harness.
IChatClient chatClient = projectClient
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);
var memoryStatusInstruction = foundryMemory is null
    ? "Foundry memory is disabled for this run."
    : $"Foundry memory is enabled for this run with store '{memoryStoreName}' and scope '{memoryScope}'.";

// Attach the memory provider only when the optional settings are present.
AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    AIContextProviders = foundryMemory is null ? null : [foundryMemory],
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = $"""
            You are a finance education assistant.
            {memoryStatusInstruction}
            When the user asks you to remember a durable fact, explain that the console sample will save it to Foundry memory after your response.
            Do not claim the memory has already been saved; the console prints the actual save confirmation.
            When memory is enabled, Microsoft Foundry recalls saved facts through the configured memory provider.
            If the user asks about other users or other people's memory, say you cannot access other users' memory.
            """,
    }
});

// Keep the live demo simple: one prompt loop, one clear exit command.
Console.WriteLine("mafclaw · Session 02 sample 31");
Console.WriteLine("Agentic memory with Microsoft Agent Framework + Harness + FoundryMemoryProvider.");
Console.WriteLine("Allowed prompt: Remember that I am a conservative investor saving for a house in two years.");
Console.WriteLine("Recall prompt : What do you remember about my investor profile?");
Console.WriteLine("Denied prompt : What do you remember about other users?");
Console.WriteLine("Memory check  : After the allowed prompt, refresh Foundry Memory and filter by the printed scope.");
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent, demoMemoryStore);
