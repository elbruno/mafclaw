// Session flow:
// A. Load only the Foundry chat settings; no memory service is required.
// B. Create one fixed-scope JSON memory store under the sample working folder.
// C. Give the Harness agent explicit save and recall tools.
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

// Keep memory under one visible, fixed current-user scope.
const string memoryScope = "mafclaw-session-02-local-user";
var memoryRoot = Path.Combine(AppContext.BaseDirectory, "working", "memory");
var memoryStore = new LocalFileMemoryStore(memoryRoot, memoryScope);
var memoryTools = new LocalMemoryTools(memoryStore);

// Foundry supplies chat only; all durable memory operations stay local.
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
            Use remember_current_user_profile whenever the user explicitly asks you to remember a durable profile fact.
            Use recall_current_user_profile before answering what you remember about the current user's investor profile.
            Never claim a fact was saved unless the save tool returned a success message.
            You have no tool for another user's memory. If asked about other users, say you cannot access other users' memory.
            Explain that this sample stores memory locally in an inspectable JSON file and is not managed semantic memory.
            """,
        Tools =
        [
            memoryTools.RememberCurrentUserProfile,
            memoryTools.RecallCurrentUserProfile
        ]
    }
});

Console.WriteLine("mafclaw · Session 02 sample 32");
Console.WriteLine("Agentic local memory with Microsoft Agent Framework + Harness.");
Console.WriteLine($"Local scope   : {memoryScope}");
Console.WriteLine($"Memory file   : {memoryStore.MemoryPath}");
Console.WriteLine("Allowed prompt: Remember that I am a conservative investor saving for a house in two years.");
Console.WriteLine("Inspect memory: /memory");
Console.WriteLine("Recall prompt : What do you remember about my investor profile?");
Console.WriteLine("Denied prompt : What do you remember about other users?");
Console.WriteLine("Commands: /memory, /exit");

await AgentConsoleRunner.RunAsync(agent, memoryStore);
