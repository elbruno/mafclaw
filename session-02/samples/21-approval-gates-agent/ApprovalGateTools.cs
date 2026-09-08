using System.ComponentModel;
using Microsoft.Extensions.AI;

internal static class ApprovalGateTools
{
    [Description("Requests human approval before placing a demo-only simulated trade.")]
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

        if (!Confirm($"Approve this simulated {normalizedSide}: {shares} shares of {normalizedSymbol}?"))
        {
            return "Denied: no trade executed.";
        }

        return $"Approved: simulated {normalizedSide} order for {shares} shares of {normalizedSymbol}. This is not a real transaction.";
    }

    public static AIFunction RequestSimulatedTrade { get; } =
        AIFunctionFactory.Create(RequestSimulatedTradeOrder, "request_simulated_trade");

    private static bool Confirm(string prompt)
    {
        Console.Write($"{prompt} [y/N]: ");
        var response = Console.ReadLine();
        return response is not null &&
            (response.Equals("y", StringComparison.OrdinalIgnoreCase) ||
             response.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }
}
