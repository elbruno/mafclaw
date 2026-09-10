// Session flow:
// A. Describe the simulated trade as an agent-callable function.
// B. Normalize the model-provided side and symbol values.
// C. Reject invalid quantities before doing any work.
// D. Wrap the function so Harness approval happens first.

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
        // Normalize model-provided text before echoing it back to the user.
        var normalizedSide = side.Trim().ToLowerInvariant();
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();

        // Validate the request before returning an approved-looking result.
        if (shares <= 0)
        {
            return "Denied: share quantity must be greater than zero.";
        }

        return $"Approved: simulated {normalizedSide} order for {shares} shares of {normalizedSymbol}. This is not a real transaction.";
    }

    // Harness asks for approval before this function can execute.
    public static AIFunction RequestSimulatedTrade { get; } =
        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(RequestSimulatedTradeOrder, "request_simulated_trade"));
}
