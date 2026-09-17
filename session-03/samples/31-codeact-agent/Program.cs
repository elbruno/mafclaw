// Objective: let a live agent read a file and compute inside a Hyperlight sandbox.
// Steps:
// A. Load Foundry settings and seed the mock holdings file.
// B. Scope host file tools and configure automatic sandbox code execution.
// C. Run the agent console and show the generated calculation.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using HyperlightSandbox.Guest.Python;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hyperlight;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

// A. Load endpoint and model settings for the live bridge.
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

// B. Give file access a fixed folder; the agent reads the CSV, then passes
// values to the sandbox rather than letting generated code access host files.
var workingDirectory = Path.Combine(AppContext.BaseDirectory, "working");
Directory.CreateDirectory(workingDirectory);
var holdingsPath = Path.Combine(workingDirectory, "holdings.csv");
if (!File.Exists(holdingsPath))
{
    File.WriteAllText(
        holdingsPath,
        "symbol,shares,price,sector\nMSFT,35,430.12,Technology\nNVDA,20,142.50,Technology\nJNJ,40,156.30,Healthcare\nXOM,25,118.75,Energy\n");
}

// Microsoft.Agents.AI.Hyperlight's HyperlightCodeActProvider turns the Python
// micro-VM into an agent capability, avoiding a custom sandbox bridge.
// This demo deliberately runs generated sandbox code without a human approval prompt.
var codeActOptions = HyperlightCodeActProviderOptions.CreateForWasm(PythonGuestModule.GetModulePath());
codeActOptions.ApprovalMode = CodeActApprovalMode.NeverRequire;
var codeAct = new HyperlightCodeActProvider(codeActOptions);

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

// HarnessAgentOptions wires file access, CodeAct, and the remaining tool approvals into one agent
// loop, instead of requiring the application to dispatch each request itself.
AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    // Keep this lesson focused: no plan-first pause or unrelated memory/todo writes.
    DisableAgentModeProvider = true,
    DisableFileMemory = true,
    DisableTodoProvider = true,
    DisableAgentSkillsProvider = true,
    DisableWebSearch = true,
    // FileSystemAgentFileStore gives MAF file_access tools a scoped root instead
    // of making the application implement file-tool registration and validation.
    FileAccessStore = new FileSystemAgentFileStore(workingDirectory),
    AIContextProviders = [codeAct],
    ToolApprovalAgentOptions = new ToolApprovalAgentOptions
    {
        // FileAccessProvider supplies a reusable rule for safe read-only calls.
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
Console.WriteLine("CODE EXECUTION POLICY: NeverRequire - generated Python runs automatically inside Hyperlight.");
Console.WriteLine("Sandbox isolation is not human review. Use only the included mock data; host file-tool rules are unchanged.");
Console.WriteLine("The agent reads holdings.csv via file_access, then writes and runs Python in a Hyperlight micro-VM to compute the answer.");
Console.WriteLine("Try: What is the total portfolio value, and what percent is in Technology?");
Console.WriteLine("Commands: /exit");

// C. Show actual tool requests/results; surface any remaining non-CodeAct approvals.
await AgentConsoleRunner.RunAsync(agent);
