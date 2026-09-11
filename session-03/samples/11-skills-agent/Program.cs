using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
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

var skillsDirectory = Path.Combine(AppContext.BaseDirectory, "skills");
var skillsProvider = new AgentSkillsProviderBuilder()
    .UseFileSkills([skillsDirectory], scriptRunner: RejectScriptExecution)
    .Build();

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    DisableAgentSkillsProvider = true,
    AIContextProviders = [skillsProvider],
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a mock portfolio education assistant.
            Discover and load a relevant skill before answering valuation or risk questions.
            Use only the skill's bundled mock resources. Do not use real market data.
            State clearly that results are educational and not financial advice.
            """,
    },
});

Console.WriteLine("Sample 11 - Microsoft Agent Framework file-based skills");
Console.WriteLine("The Harness advertises skill names/descriptions, then loads SKILL.md content on demand.");
Console.WriteLine("Try: Value 25 shares of MSFT using the mock data.");
Console.WriteLine("Try: What risk does a 55% NVDA allocation create?");
Console.WriteLine("Commands: /exit");

await AgentConsoleRunner.RunAsync(agent);

// This sample's SKILL.md files only bundle instructions and reference data, so no
// script ever runs. A real skill package with a scripts/ folder would execute it here
// (for example, via a subprocess) instead of throwing.
static Task<object?> RejectScriptExecution(
    AgentFileSkill skill,
    AgentFileSkillScript script,
    JsonElement? arguments,
    IServiceProvider? serviceProvider,
    CancellationToken cancellationToken) =>
    Task.FromException<object?>(new NotSupportedException(
        $"Skill '{skill.Frontmatter.Name}' has no runnable scripts in this sample."));
