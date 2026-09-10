// Session flow:
// A. Describe the simulated trade as an agent-callable function.
// B. Normalize and validate model-provided values.
// C. Return a demo-only result after approval succeeds.
// D. Wrap the function so Harness requests approval before execution.

using System.ComponentModel;
using Microsoft.Extensions.AI;

internal static class ApprovalGateTools
{
    [Description("Places a demo-only simulated trade after the Harness approval flow allows it.")]
    public static string RequestSimulatedTradeOrder(
        [Description("Trade side, for example buy or sell.")] string side,
        [Description("Ticker symbol, for example MSFT.")] string symbol,
        [Description("Number of shares.")] int shares)
    {
        // Normalize model-provided text before echoing it back.
        var normalizedSide = side.Trim().ToLowerInvariant();
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();

        if (shares <= 0)
        {
            return "Denied: share quantity must be greater than zero.";
        }

        return $"Approved: simulated {normalizedSide} order for {shares} shares of {normalizedSymbol}. This is not a real transaction.";
    }

    // Harness pauses before this function and delegates the decision to the console policy.
    public static AIFunction RequestSimulatedTrade { get; } =
        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(RequestSimulatedTradeOrder, "request_simulated_trade"));
}
