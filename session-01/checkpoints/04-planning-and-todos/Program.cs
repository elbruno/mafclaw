using MafClaw.Checkpoint04;

return await RunAsync();

static async Task<int> RunAsync()
{
    try
    {
        var settings = FoundryConfiguration.Resolve();
        var stockTools = new StockTools(ResolveMarketDataPath());
        var runtime = await new FinanceAgentFactory(settings, stockTools)
            .CreateAsync(CancellationToken.None);

        var console = new ClawConsole(runtime.Agent, runtime.TodoProvider);
        await console.RunAsync(
            Console.In,
            Console.Out,
            CancellationToken.None);

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
