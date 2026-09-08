using System.ComponentModel;
using Microsoft.Extensions.AI;

internal static class AgentFinanceTools
{
    [Description("Places a demo-only simulated trade after the Harness approval flow allows it.")]
    public static string RequestSimulatedTradeOrder(
        [Description("Trade side, for example buy or sell.")] string side,
        [Description("Ticker symbol, for example MSFT.")] string symbol,
        [Description("Number of shares.")] int shares)
    {
        var normalizedSide = side.Trim().ToLowerInvariant();
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        if (shares <= 0)
        {
            return "Denied: share quantity must be greater than zero.";
        }

        return $"Approved: simulated {normalizedSide} order for {shares} shares of {normalizedSymbol}. This is not a real transaction.";
    }

    public static AIFunction RequestSimulatedTrade { get; } =
        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(RequestSimulatedTradeOrder, "request_simulated_trade"));
}
