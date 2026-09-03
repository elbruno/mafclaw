using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>().AddEnvironmentVariables().Build();
var endpoint = config["Foundry:ProjectEndpoint"]!;
var model = config["Foundry:Model"] ?? "gpt-5-mini";

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    DisableFileMemory = true,
    ChatOptions = new ChatOptions
    {
        Instructions = """
            You are a personal finance education assistant.
            Use get_stock_price for stock prices.
            Use hosted web search for recent market news and cite sources.
            Use the todo list to track multi-step work.
            Keep responses concise. Never provide personalized financial advice.
            """,
        Tools = [StockTools.GetStockPrice]
    }
});

var session = await agent.CreateSessionAsync();
var todos = agent.GetService<TodoProvider>()!;

Console.WriteLine("Finance assistant ready. Commands: /todos, /exit");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (input is null || input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
        break;

    if (input.Trim().StartsWith("/todos", StringComparison.OrdinalIgnoreCase))
    {
        var items = await todos.GetAllTodosAsync(session);
        if (items.Count == 0) { Console.WriteLine("No todos yet."); continue; }
        foreach (var t in items)
            Console.WriteLine($"  [{(t.IsComplete ? "x" : " ")}] {t.Title}");
        continue;
    }

    var response = await agent.RunAsync(input, session);
    Console.WriteLine(response.Text);
}
