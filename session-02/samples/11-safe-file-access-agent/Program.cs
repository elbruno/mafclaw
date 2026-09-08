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

var workingDirectory = Path.Combine(AppContext.BaseDirectory, "working");
Directory.CreateDirectory(workingDirectory);

var portfolioPath = Path.Combine(workingDirectory, "portfolio.csv");
if (!File.Exists(portfolioPath))
{
    File.WriteAllText(
        portfolioPath,
        "symbol,shares,averageCost,risk\nMSFT,25,430.10,moderate\nSPY,40,530.25,low\nNVDA,18,142.50,high\n");
}

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
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
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a finance education assistant.
            The user's mock portfolio is in portfolio.csv.
            Use the built-in file_access tools to inspect files before answering portfolio questions.
            Read-only file operations are allowed automatically; writes and destructive file operations require approval.
            Do not invent portfolio data and do not request data outside the approved working folder.
            Remind the user that the portfolio is mock educational data.
            """,
    }
});

var session = await agent.CreateSessionAsync();

Console.WriteLine("mafclaw · Session 02 sample 11");
Console.WriteLine("Agentic safe file access with Microsoft Agent Framework + Harness.");
Console.WriteLine("Try: What is in my portfolio?");
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
