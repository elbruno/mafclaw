// Objective: keep hosted research request-scoped rather than leaving background jobs behind.
// A. Validate a small set of synthetic ticker inputs.
// B. Run the same MAF research worker with a shared deadline.
// C. Await every result before returning to the hosted request.
using System.ComponentModel;
using Microsoft.Agents.AI;

namespace MafClaw.Session04;

public sealed class HostedResearchTools(AIAgent worker)
{
    [Description("Research up to three allowed educational tickers, awaiting all workers before returning.")]
    public async Task<string[]> ResearchAsync(string[] symbols, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        if (symbols.Length is < 1 or > 3 || symbols.Any(symbol =>
            !MockPortfolio.Holdings.Any(row => row.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("Research accepts one to three tickers from the synthetic portfolio.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(60));
        return await Task.WhenAll(symbols.Select(async symbol =>
        {
            var session = await worker.CreateSessionAsync(deadline.Token);
            var response = await worker.RunAsync(
                $"Research public news for {symbol.ToUpperInvariant()}. Cite sources; do not invent news.", session,
                cancellationToken: deadline.Token);
            return response.Text;
        }));
    }
}
