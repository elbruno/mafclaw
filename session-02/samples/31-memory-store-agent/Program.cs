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

MemoryStoreTools.Initialize(Path.Combine(AppContext.BaseDirectory, "memory.json"));

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a finance education assistant.
            Use remember_user_preference to store preferences.
            Use get_memory to read stored values.
            Explain that this sample uses simple local file memory before the full app expands the pattern.
            """,
        Tools =
        [
            MemoryStoreTools.RememberUserPreference,
            MemoryStoreTools.GetMemory
        ]
    }
});

var session = await agent.CreateSessionAsync();

Console.WriteLine("mafclaw · Session 02 sample 31");
Console.WriteLine("Agentic memory store with Microsoft Agent Framework + Harness.");
Console.WriteLine("Try: Remember that I am a conservative investor.");
Console.WriteLine("Then restart and ask: What do you remember about my user-preference?");
Console.WriteLine("Commands: /exit");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (input is null || input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    var response = await agent.RunAsync(input, session);
    Console.WriteLine(response.Text);
}
