// Objective: preserve useful mock-finance tools and an observable approval target.
// A. Return synthetic quotes and the fixed portfolio snapshot.
// B. Validate simulated trade parameters.
// C. Register side effects through MAF's ApprovalRequiredAIFunction.
using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04;

public sealed class FinanceTools
{
    private readonly Lock gate = new();
    private readonly List<string> trades = [];
    public IReadOnlyList<string> ExecutedTrades { get { lock (gate) return trades.ToArray(); } }

    [Description("Read a synthetic stock quote. This is not real market data.")]
    public string GetStockPrice(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        using var activity = FinanceTelemetry.Activities.StartActivity("finance.quote");
        FinanceTelemetry.ToolCalls.Add(1, new KeyValuePair<string, object?>("tool.name", "get_stock_price"));
        var price = symbol.Trim().ToUpperInvariant() switch
        {
            "MSFT" => 512.34m, "NVDA" => 142.50m, "JNJ" => 156.30m, "XOM" => 118.75m,
            _ => throw new ArgumentException("Unknown synthetic stock symbol.")
        };
        return FormattableString.Invariant($"{symbol.Trim().ToUpperInvariant()}: {price:F2} USD (mock)");
    }

    [Description("Read the fixed four-row educational portfolio snapshot.")]
    public IReadOnlyList<Holding> GetPortfolio() => MockPortfolio.Holdings;

    [Description("Calculate the fixed snapshot total and Technology allocation using decimal arithmetic.")]
    public PortfolioSummary ValuePortfolio()
    {
        using var activity = FinanceTelemetry.Activities.StartActivity("finance.valuation");
        FinanceTelemetry.ToolCalls.Add(1, new KeyValuePair<string, object?>("tool.name", "value_portfolio"));
        return MockPortfolio.Summarize();
    }

    [Description("Propose an in-memory SIMULATED trade. The host requests human approval before execution. No real trade is placed.")]
    public string SimulateTrade(string side, string symbol, int shares)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (side.ToLowerInvariant() is not ("buy" or "sell") || shares is < 1 or > 1000)
            throw new ArgumentException("Use buy/sell and 1-1000 synthetic shares.");
        if (!MockPortfolio.Holdings.Any(row => row.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Unknown synthetic stock symbol.");
        var result = $"SIMULATED {side.ToLowerInvariant()} {shares} {symbol.ToUpperInvariant()}. Not a real transaction.";
        lock (gate) trades.Add(result);
        return result;
    }

    public IList<AITool> CreateTools(bool interactive)
    {
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(GetStockPrice, "get_stock_price"),
            AIFunctionFactory.Create(GetPortfolio, "get_portfolio"),
            AIFunctionFactory.Create(ValuePortfolio, "value_portfolio")
        };
        if (interactive)
            tools.Add(new ApprovalRequiredAIFunction(AIFunctionFactory.Create(SimulateTrade, "request_simulated_trade")));
        return tools;
    }
}
