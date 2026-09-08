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

SafeFileAccessTools.Initialize(Path.Combine(AppContext.BaseDirectory, "working"));

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
            Use read_portfolio_summary to answer portfolio questions.
            Do not invent portfolio data and do not read outside the approved working folder.
            Remind the user that the portfolio is mock educational data.
            """,
        Tools =
        [
            SafeFileAccessTools.ReadPortfolioSummary
        ]
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
