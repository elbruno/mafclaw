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

AgentFinanceTools.Initialize(Path.Combine(AppContext.BaseDirectory, "working"));

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a personal finance education assistant for a live coding workshop.
            Use the provided tools instead of inventing portfolio data.
            Explain that all values are mock educational data, not financial advice.

            Important safety boundaries:
            - Read portfolio data with read_portfolio_summary.
            - Write reports only through write_portfolio_report.
            - Remember user preferences with remember_user_preference.
            - Read memory with get_memory.
            - Simulated trades must go through request_simulated_trade, which asks the human for approval.
            """,
        Tools =
        [
            AgentFinanceTools.ReadPortfolioSummary,
            AgentFinanceTools.WritePortfolioReport,
            AgentFinanceTools.RememberUserPreference,
            AgentFinanceTools.GetMemory,
            AgentFinanceTools.RequestSimulatedTrade
        ]
    }
});

var session = await agent.CreateSessionAsync();

Console.WriteLine("mafclaw · Session 02");
Console.WriteLine("Agent Framework + Harness finance advisor");
Console.WriteLine("This app turns the safe file, approval, and memory concepts into agent tools.");
Console.WriteLine();
Console.WriteLine("Try these prompts:");
Console.WriteLine("  What is in my portfolio?");
Console.WriteLine("  Write a short markdown report about my portfolio.");
Console.WriteLine("  Remember that I am a conservative investor saving for a house in two years.");
Console.WriteLine("  What do you remember about my investor profile?");
Console.WriteLine("  Buy 10 shares of MSFT.");
Console.WriteLine();
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
