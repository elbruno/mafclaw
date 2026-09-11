// Session flow:
// A. Load the Foundry connection settings.
// B. Mount a read-only holdings CSV into a Hyperlight micro-VM sandbox.
// C. Expose a single execute_code tool so the model writes and runs its own JavaScript
//    over the mounted data, instead of calling a fixed pre-written function.
// D. Every code execution requires approval before it runs inside the sandbox.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using HyperlightSandbox.Guest.Python;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hyperlight;
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

// The file_access tools are scoped to this folder only; CodeAct never touches disk directly -
// the agent reads holdings.csv with file_access, then hands the numbers to the sandbox to compute.
var workingDirectory = Path.Combine(AppContext.BaseDirectory, "working");
Directory.CreateDirectory(workingDirectory);
var holdingsPath = Path.Combine(workingDirectory, "holdings.csv");
if (!File.Exists(holdingsPath))
{
    File.WriteAllText(
        holdingsPath,
        "symbol,shares,price,sector\nMSFT,35,430.12,Technology\nNVDA,20,142.50,Technology\nJNJ,40,156.30,Healthcare\nXOM,25,118.75,Energy\n");
}

var codeActOptions = HyperlightCodeActProviderOptions.CreateForWasm(PythonGuestModule.GetModulePath());
codeActOptions.ApprovalMode = CodeActApprovalMode.AlwaysRequire;
var codeAct = new HyperlightCodeActProvider(codeActOptions);

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    FileAccessStore = new FileSystemAgentFileStore(workingDirectory),
    AIContextProviders = [codeAct],
    ToolApprovalAgentOptions = new ToolApprovalAgentOptions
    {
        AutoApprovalRules = [FileAccessProvider.ReadOnlyToolsAutoApprovalRule],
    },
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a finance-education assistant. The user's mock portfolio is in holdings.csv
            (columns: symbol,shares,price,sector). Use the file_access tools to read it - reads
            are auto-approved. Never do the arithmetic yourself: write Python and run it with the
            sandbox's execute-code tool, then report the code's printed output. Show your work.
            """,
    },
});

Console.WriteLine("Sample 31 - MAF CodeAct bridge (Hyperlight sandbox)");
Console.WriteLine("The agent reads holdings.csv via file_access, then writes and runs Python in a Hyperlight micro-VM to compute the answer.");
Console.WriteLine("Try: What is the total portfolio value, and what percent is in Technology?");
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent);


