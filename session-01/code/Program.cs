using System.Text.Json;

var sample = new Session01Sample(
    ResolveMarketDataPath(),
    new ConsoleHarness());

Console.WriteLine("mafclaw · Session 01 snapshot");
Console.WriteLine("Illustrative sample only. Mock data only. Not financial advice.");
Console.WriteLine();

await sample.RunAsync();

static string ResolveMarketDataPath()
{
    var outputPath = Path.Combine(AppContext.BaseDirectory, "mock-market-data.json");
    if (File.Exists(outputPath))
    {
        return outputPath;
    }

    return Path.Combine(Directory.GetCurrentDirectory(), "mock-market-data.json");
}

internal sealed class Session01Sample(string marketDataPath, IAgentHarnessFactory harnessFactory)
{
    private readonly string _marketDataPath = marketDataPath;
    private readonly IAgentHarnessFactory _harnessFactory = harnessFactory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task RunAsync()
    {
        var claw = BuildHarness().AsHarnessAgent();

        Console.WriteLine($"Harness agent ready: {claw.Name}");
        Console.WriteLine();

        var prompt = "Plan my next moves for MSFT and NVDA, and look up market mood.";
        Console.WriteLine($"Prompt: {prompt}");
        Console.WriteLine();

        var plan = new List<string>
        {
            "Load mock portfolio context",
            "Call get_stock_price for requested symbols",
            "Call web_search for lightweight market mood context",
            "Return a short todo-style answer"
        };

        Console.WriteLine("Todo planning");
        foreach (var item in plan)
        {
            Console.WriteLine($"- {item}");
        }

        Console.WriteLine();

        var msft = await GetStockPriceAsync("MSFT");
        var nvda = await GetStockPriceAsync("NVDA");
        var marketMood = await WebSearchAsync("latest market mood for major tech stocks");

        var result = new
        {
            agent = claw.Name,
            tools = new[] { "get_stock_price", "web_search", "todo_list" },
            prices = new[] { msft, nvda },
            webSearch = marketMood,
            todo = new[]
            {
                $"Review {msft.Symbol} at {msft.Price} {msft.Currency} from mock data.",
                $"Review {nvda.Symbol} at {nvda.Price} {nvda.Currency} from mock data.",
                "Compare the mock prices with the web-search headline before making any live-demo claims.",
                "Keep the sample focused on harness plus tools, not trading advice."
            }
        };

        Console.WriteLine("Sample response");
        Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
    }

    private HarnessDefinition BuildHarness()
    {
        return _harnessFactory.Create("mafclaw-session-01", new[]
        {
            "get_stock_price",
            "web_search",
            "todo_list"
        });
    }

    private async Task<StockQuote> GetStockPriceAsync(string symbol)
    {
        await using var stream = File.OpenRead(_marketDataPath);
        var quotes = await JsonSerializer.DeserializeAsync<List<StockQuote>>(stream, _jsonOptions)
            ?? throw new InvalidOperationException("Mock market data is missing.");

        var match = quotes.SingleOrDefault(q => string.Equals(q.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
        return match ?? throw new InvalidOperationException($"No mock quote found for {symbol}.");
    }

    private Task<WebSearchResult> WebSearchAsync(string query)
    {
        return Task.FromResult(new WebSearchResult(
            query,
            "Mocked web search summary: tech sentiment is cautious but positive heading into the open.",
            "placeholder-safe"));
    }
}

internal interface IAgentHarnessFactory
{
    HarnessDefinition Create(string name, IReadOnlyList<string> toolNames);
}

internal sealed class ConsoleHarness : IAgentHarnessFactory
{
    public HarnessDefinition Create(string name, IReadOnlyList<string> toolNames)
    {
        return new HarnessDefinition(name, toolNames);
    }
}

internal sealed record HarnessDefinition(string Name, IReadOnlyList<string> ToolNames)
{
    public HarnessAgent AsHarnessAgent()
    {
        return new HarnessAgent(Name, ToolNames);
    }
}

internal sealed record HarnessAgent(string Name, IReadOnlyList<string> ToolNames);

internal sealed record StockQuote(string Symbol, decimal Price, string Currency, string Source);

internal sealed record WebSearchResult(string Query, string Summary, string Source);
