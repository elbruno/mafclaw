namespace MafClaw.Session01;

internal sealed class OfflineClaw(StockTools stockTools)
{
    private readonly StockTools _stockTools = stockTools;

    public async Task RunAsync(
        string? scenario,
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        await output.WriteLineAsync("OFFLINE FALLBACK");
        await output.WriteLineAsync("Offline mode uses only local fixtures. No Azure, network, or Foundry config is used.");

        if (!string.IsNullOrWhiteSpace(scenario))
        {
            await RunScenarioAsync(scenario, output, cancellationToken);
            return;
        }

        await output.WriteLineAsync("Commands: /stock <SYMBOL>, /plan, /exit");

        while (true)
        {
            await output.WriteAsync("offline> ");
            await output.FlushAsync();

            var line = await input.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (string.Equals(trimmed, "/exit", StringComparison.OrdinalIgnoreCase))
            {
                await output.WriteLineAsync("Exiting offline mode.");
                break;
            }

            if (string.Equals(trimmed, "/plan", StringComparison.OrdinalIgnoreCase))
            {
                await RunScenarioAsync("plan", output, cancellationToken);
                continue;
            }

            if (trimmed.StartsWith("/stock ", StringComparison.OrdinalIgnoreCase))
            {
                var symbol = trimmed["/stock ".Length..];
                await LookUpStockAsync(symbol, output);
                continue;
            }

            await output.WriteLineAsync("Unknown command. Use /stock <SYMBOL>, /plan, or /exit.");
        }
    }

    internal async Task LookUpStockAsync(string symbol, TextWriter output)
    {
        try
        {
            var quote = _stockTools.GetStockPrice(symbol);
            await output.WriteLineAsync($"{quote.Symbol}: {quote.Price:0.00} {quote.Currency} ({quote.Source})");
        }
        catch (ArgumentException ex)
        {
            await output.WriteLineAsync($"Invalid symbol '{symbol.Trim()}': {ex.Message}");
        }
        catch (KeyNotFoundException)
        {
            var upper = symbol.Trim().ToUpperInvariant();
            await output.WriteLineAsync($"Symbol '{upper}' not found in offline data. Use /stock <SYMBOL> with a supported ticker.");
        }
    }

    internal async Task RunScenarioAsync(
        string scenario,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var normalizedScenario = scenario.Trim().ToLowerInvariant();
        switch (normalizedScenario)
        {
            case "stock":
            {
                var msft = _stockTools.GetStockPrice("MSFT");
                var nvda = _stockTools.GetStockPrice("NVDA");
                await output.WriteLineAsync("SCENARIO stock");
                await output.WriteLineAsync($"{msft.Symbol}: {msft.Price:0.00} {msft.Currency} ({msft.Source})");
                await output.WriteLineAsync($"{nvda.Symbol}: {nvda.Price:0.00} {nvda.Currency} ({nvda.Source})");
                return;
            }
            case "plan":
            {
                await output.WriteLineAsync("SCENARIO plan");
                var planning = new PlanningResponse
                {
                    Type = PlanningResponseType.Approval,
                    Questions =
                    [
                        new PlanningQuestion
                        {
                            Message = "1) Fetch MSFT and NVDA prices. 2) Gather market mood with hosted search. 3) Summarize risks."
                        }
                    ]
                };

                var executionTriggered = false;
                PlanApprovalGate.TryExecute(planning, isApproved: false, () => executionTriggered = true);

                await output.WriteLineAsync("Approval granted: no");
                await output.WriteLineAsync($"Execution triggered: {(executionTriggered ? "yes" : "no")}");
                await output.WriteLineAsync("Execution blocked until explicit approval.");
                return;
            }
            default:
                throw new InvalidOperationException("Offline scenario must be 'stock' or 'plan'.");
        }
    }
}
