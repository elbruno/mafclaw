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
        Instructions = "You are a personal finance education assistant. Use get_stock_price for stock prices. Keep responses concise.",
        Tools = [StockTools.GetStockPrice]
    }
});

Console.WriteLine(await agent.RunAsync(
    "What is the current price of MSFT?"));
