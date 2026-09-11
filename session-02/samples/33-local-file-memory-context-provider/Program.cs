// Session flow:
// A. Load only the Foundry chat settings; no Foundry memory service is required.
// B. Create one local FileMemoryProvider variable scoped to the current demo user.
// C. Attach that local memory provider to the Harness agent through AIContextProviders.
// D. Demonstrate save, inspect, restart, recall, and cross-user refusal.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

// Read chat configuration from user-secrets first, then environment variables.
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

// Keep provider-backed local memory under one visible, fixed current-user scope.
const string memoryScope = "mafclaw-session-02-local-user";
var memoryRoot = Path.Combine(AppContext.BaseDirectory, "working", "memory");
var scopedMemoryDirectory = Path.Combine(memoryRoot, memoryScope);
Directory.CreateDirectory(scopedMemoryDirectory);

var localFileMemory = new FileMemoryProvider(
    new FileSystemAgentFileStore(memoryRoot),
    _ => new FileMemoryState { WorkingFolder = memoryScope },
    new FileMemoryProviderOptions
    {
        Instructions = """
            You have local file-backed memory tools for the current user's investing profile.
            Use file_memory_write when the user explicitly asks you to remember a durable current-user profile fact.
            Use file_memory_read or file_memory_ls before answering what you remember about the current user's investor profile.
            Store current-user profile facts in profile.md.
            The memory provider is already scoped to the current user. Never ask for, create, or read another user's memory folder.
            If asked about other users, say you cannot access other users' memory.
            """,
    });

// Foundry supplies chat only; all durable memory operations stay in local files.
IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

// Attach the local memory provider through the same AIContextProviders hook used by FoundryMemoryProvider.
AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    AIContextProviders = [localFileMemory],
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a finance education assistant.
            Local memory is enabled through FileMemoryProvider and stored in inspectable files on disk.
            When the user asks you to remember a durable fact, save it with the local file memory tools.
            Never claim a fact was saved unless the file memory tool returned a success message.
            When the user asks what you remember, read local file memory before answering.
            If the user asks about other users or other people's memory, say you cannot access other users' memory.
            Explain that this sample stores memory locally in inspectable files and is not managed semantic memory.
            """,
    }
});

Console.WriteLine("mafclaw · Session 02 sample 33");
Console.WriteLine("Local FileMemoryProvider with Microsoft Agent Framework + Harness.");
Console.WriteLine($"Local scope   : {memoryScope}");
Console.WriteLine($"Memory folder : {scopedMemoryDirectory}");
Console.WriteLine("Allowed prompt: Remember that I am a conservative investor saving for a house in two years.");
Console.WriteLine("Inspect memory: /memory");
Console.WriteLine("Recall prompt : What do you remember about my investor profile?");
Console.WriteLine("Denied prompt : What do you remember about other users?");
Console.WriteLine("Commands: /memory, /exit");

await AgentConsoleRunner.RunAsync(agent, scopedMemoryDirectory);
