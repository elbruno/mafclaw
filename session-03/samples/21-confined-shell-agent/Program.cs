// Objective: show a model-proposed rename, explicit approval and host-verified results.
// Steps:
// A. Load Foundry settings and prepare a fresh, preserved demo workspace.
// B. Give MAF an explicit PowerShell executor, shell context and approval-gated tool.
// C. Run the conversation and show real tool results plus independent verification.

using System.ComponentModel;
using System.ClientModel;
using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using MafClaw.Sample21;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Tools.Shell;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

// A. Configuration stays local; never print endpoints or credentials on a livestream.
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var endpoint = configuration["Foundry:ProjectEndpoint"] ?? configuration["FOUNDRY_PROJECT_ENDPOINT"];
var model = configuration["Foundry:Model"] ?? configuration["FOUNDRY_MODEL"] ?? "gpt-5-mini";

if (string.IsNullOrWhiteSpace(endpoint))
{
    Console.Error.WriteLine("Missing Foundry:ProjectEndpoint. Configure Session 3 before presenting.");
    Console.Error.WriteLine(@".\tools\configure-user-secrets.ps1 -Session 3");
    return 1;
}

try
{
    var workspace = DemoWorkspace.Create(
        Path.Combine(AppContext.BaseDirectory, "working", "confirmations"));

    // B. LocalShellExecutor owns process startup, output capture and the timeout.
    // ConfineWorkingDirectory re-anchors each command; it is NOT a filesystem sandbox.
    await using var shell = new LocalShellExecutor(new LocalShellExecutorOptions
    {
        Shell = "pwsh",
        WorkingDirectory = workspace.DirectoryPath,
        ConfineWorkingDirectory = true,
        Timeout = TimeSpan.FromSeconds(15),
        MaxOutputBytes = 4096,
        // This prefilter catches obvious off-task commands, not every possible unsafe script.
        Policy = new ShellPolicy(denyList:
        [
            @"\b(?:Remove-Item|Clear-Content|Set-Content|Add-Content|Out-File)\b",
            @"\b(?:rm|del|erase|rmdir|rd|sudo|mkfs)\b"
        ])
    });

    // MAF probes the real executor and supplies its dialect/version to the model.
    // We do not implement a second shell-detection or context-provider mechanism.
    var shellContext = new ShellEnvironmentProvider(shell, new ShellEnvironmentProviderOptions
    {
        OverrideFamily = ShellFamily.PowerShell,
        ProbeTools = []
    });
    var environment = await shellContext.RefreshAsync();
    if (string.IsNullOrWhiteSpace(environment.ShellVersion))
    {
        throw new InvalidOperationException("PowerShell probe failed. Install PowerShell 7 and put pwsh on PATH.");
    }

    // AsAIFunction keeps approval in the MAF protocol, including read-only shell calls.
    var runShell = shell.AsAIFunction(
        "run_shell", "Run PowerShell in the current mock-confirmations workspace.", requireApproval: true);

    // AIProjectClient connects Foundry to the IChatClient abstraction used by MAF.
    using IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
        .GetProjectOpenAIClient()
        .GetResponsesClient()
        .AsIChatClient(model);

    // AsHarnessAgent owns model/tool routing and approval responses. This lesson needs
    // only shell context and run_shell, not unrelated memory, skills, todos or web tools.
    AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
    {
        AIContextProviders = [shellContext],
        DisableAgentSkillsProvider = true,
        DisableFileMemory = true,
        DisableTodoProvider = true,
        DisableWebSearch = true,
        DisableAgentModeProvider = true,
        DisableToolAutoApproval = true,
        ChatOptions = new ChatOptions { Tools = [runShell], Instructions = DemoInstructions.Text }
    });

    Console.WriteLine("Sample 21 - inspect, propose, approve, execute, verify");
    Console.WriteLine($"Shell: {shell.ResolvedShellBinary} (PowerShell {environment.ShellVersion})");
    Console.WriteLine("Every model-proposed shell command requires approval. This is not an OS sandbox.");
    workspace.WriteBefore(Console.Out);
    Console.WriteLine("Try: Tidy up my trade confirmations.");
    Console.WriteLine("Commands: /verify (host check, no model call), /exit");

    // C. The console reports actual FunctionResultContent; the host checks file hashes.
    await AgentConsoleRunner.RunAsync(agent, workspace);
    return 0;
}
catch (AuthenticationFailedException)
{
    Console.Error.WriteLine("Azure authentication failed. Sign in privately before presenting.");
    return 1;
}
catch (RequestFailedException exception)
{
    Console.Error.WriteLine($"Foundry request failed (HTTP {exception.Status}, code {exception.ErrorCode}). Check configuration privately.");
    return 1;
}
catch (ClientResultException exception)
{
    Console.Error.WriteLine($"Model request failed (HTTP {exception.Status}). Check configuration privately.");
    return 1;
}
catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
{
    Console.Error.WriteLine($"Demo stopped: {exception.Message}");
    return 1;
}
