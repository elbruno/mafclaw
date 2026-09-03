#pragma warning disable OPENAI001
#pragma warning disable MAAI001

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

return await RunAsync();

static async Task<int> RunAsync()
{
    const string instructions =
        """
        You are a personal finance and investing education assistant.
        Use get_stock_price for stock numbers.
        Use hosted web search for recent market news and cite sources inline.
        Clearly distinguish the local illustrative quote from current web information.
        Keep responses concise and never provide personalized financial advice.
        """;

    const string prompt =
        """
        Show the illustrative MSFT price using get_stock_price.
        Then find recent NVDA news using hosted web search and include inline source citations.
        """;

    try
    {
        var settings = FoundryConfiguration.Resolve();
        var stockTools = new StockTools(ResolveMarketDataPath());
        var projectClient = new AIProjectClient(
            new Uri(settings.ProjectEndpoint),
            new AzureCliCredential());

        IChatClient chatClient = projectClient
            .GetProjectOpenAIClient()
            .GetResponsesClient()
            .AsIChatClient(settings.Model);

        AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
        {
            Name = "mafclaw-tools-and-search",
            DisableCompaction = true,
            DisableFileMemory = true,
            DisableWebSearch = false,
            DisableTodoProvider = true,
            DisableAgentModeProvider = true,
            DisableAgentSkillsProvider = true,
            DisableOpenTelemetry = true,
            DisableToolAutoApproval = true,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Tools = [stockTools.CreateGetStockPriceTool()]
            }
        });

        Console.WriteLine("LIVE · checkpoint 03 · tools and search");
        var response = await agent.RunAsync(prompt);
        Console.WriteLine(response.Text);

        var webSearchUsed = response.Messages
            .SelectMany(message => message.Contents)
            .Any(content => content is WebSearchToolCallContent or WebSearchToolResultContent);

        Console.WriteLine(webSearchUsed
            ? "[Hosted web search was used.]"
            : "[Hosted web search was not used for this response.]");

        return 0;
    }
    catch (Exception exception)
    {
        return SafeErrors.Write(exception);
    }
}

static string ResolveMarketDataPath()
{
    var outputPath = Path.Combine(AppContext.BaseDirectory, "mock-market-data.json");
    if (File.Exists(outputPath))
    {
        return outputPath;
    }

    throw new FileNotFoundException("The local mock market data fixture was not found.");
}
