// Session flow:
// A. Load the Foundry connection settings and create inside/outside demo files.
// B. Build an IChatClient and expose the Harness file-access tools.
// C. Auto-approve reads while keeping writes behind approval.
// D. Ask one allowed path question and one denied path question.

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

// These values point the sample at the Azure AI Foundry project and model.
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

// This folder is the only file root the Harness agent will be allowed to use.
var workingDirectory = Path.Combine(AppContext.BaseDirectory, "working");
Directory.CreateDirectory(workingDirectory);

// Keep the demo data inside the same root exposed to the agent.
var portfolioPath = Path.Combine(workingDirectory, "portfolio.csv");
if (!File.Exists(portfolioPath))
{
    File.WriteAllText(
        portfolioPath,
        "symbol,shares,averageCost,risk\nMSFT,25,430.10,moderate\nSPY,40,530.25,low\nNVDA,18,142.50,high\n");
}

// Create a harmless decoy file outside the approved root for the denial demo.
var deniedDirectory = Path.Combine(Path.GetTempPath(), "mafclaw-session-02-outside-root");
Directory.CreateDirectory(deniedDirectory);
var deniedPath = Path.Combine(deniedDirectory, "outside-portfolio.csv");
File.WriteAllText(deniedPath, "symbol,shares\nPRIVATE,999\n");

// Adapt the Foundry project client into the chat client shape expected by Harness.
IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

// Create a Harness agent and give it scoped file tools instead of raw disk access.
AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    FileAccessStore = new FileSystemAgentFileStore(workingDirectory),
    ToolApprovalAgentOptions = new ToolApprovalAgentOptions
    {
        AutoApprovalRules = [FileAccessProvider.ReadOnlyToolsAutoApprovalRule],
    },
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a finance education assistant.
            The user's mock portfolio is in portfolio.csv.
            Use the built-in file_access tools to inspect files before answering portfolio questions.
            Read-only file operations are allowed automatically; writes and destructive file operations require approval.
            Do not invent portfolio data and do not request data outside the approved working folder.
            If the user asks for a path outside the approved working folder, say: "I can't access that folder because it is outside the approved working folder."
            Remind the user that the portfolio is mock educational data.
            """,
    }
});

// Keep the live demo simple: one prompt loop, one clear exit command.
Console.WriteLine("mafclaw · Session 02 sample 11");
Console.WriteLine("Agentic safe file access with Microsoft Agent Framework + Harness.");
Console.WriteLine("Allowed prompt: What is in my portfolio?");
Console.WriteLine($"Denied prompt : Read {deniedPath}");
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent);
