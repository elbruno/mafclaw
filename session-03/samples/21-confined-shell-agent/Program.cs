// Objective: expose a confined shell executor as an approval-gated agent tool.
// Steps:
// A. Load Foundry settings and seed mock confirmations.
// B. Confine shell execution and require approval.
// C. Run the live agent console.

// Session flow:
// A. Load the Foundry connection settings.
// B. Confine a shell executor to a scratch "confirmations" folder with a deny-list policy.
// C. Attach the shell as the Harness agent's approval-gated run_shell tool.
// D. Ask the agent to tidy up the messy confirmation files.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Tools.Shell;
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

// Every shell command is re-anchored to this folder and cannot escape it.
// B. Seed and confine the mock confirmation folder.
var vaultDir = Path.Combine(AppContext.BaseDirectory, "working", "confirmations");
Directory.CreateDirectory(vaultDir);
SeedMessyConfirmations(vaultDir);

await using var shell = new LocalShellExecutor(new LocalShellExecutorOptions
{
    WorkingDirectory = vaultDir,
    ConfineWorkingDirectory = true,
    Policy = new ShellPolicy(denyList:
    [
        @"\brm\s+-rf\b", @"\bsudo\b", @":\(\)\s*\{", @"\bmkfs\b", @">\s*/dev/sd",
    ]),
    Timeout = TimeSpan.FromSeconds(15),
});
var runShell = shell.AsAIFunction(
    "run_shell",
    "Run a shell command confined to the trade-confirmations working directory.",
    requireApproval: true);

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Tools = [runShell],
        Instructions = """
            You are a finance-education assistant that tidies mock trade confirmation files.
            Use the run_shell tool to inspect and reorganize files under the confined working
            directory only. Propose a plan before running destructive or renaming commands.
            Never invent files or claim a command ran if it was denied.
            State clearly that this is mock educational data, not real trade records.
            """,
    },
});

Console.WriteLine("Sample 21 - Microsoft Agent Framework confined shell");
Console.WriteLine("The Harness exposes an approval-gated run_shell tool confined to one working folder.");
Console.WriteLine("Try: Tidy up my trade confirmations.");
Console.WriteLine("Commands: /exit");

// C. Let the agent propose commands while the console owns approval.
await AgentConsoleRunner.RunAsync(agent);

static void SeedMessyConfirmations(string vaultDir)
{
    var files = new (string Name, string Content)[]
    {
        ("trade confirmation 1.txt", "MSFT BUY 10 shares - mock confirmation"),
        ("conf_AAPL.txt", "AAPL SELL 5 shares - mock confirmation"),
        ("copy of trade 3.txt", "NVDA BUY 8 shares - mock confirmation"),
        ("SPY sell.txt", "SPY SELL 12 shares - mock confirmation"),
    };

    foreach (var (name, content) in files)
    {
        var path = Path.Combine(vaultDir, name);
        if (!File.Exists(path))
        {
            File.WriteAllText(path, content);
        }
    }
}
