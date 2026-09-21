// Objective: preserve the Session 3 snapshot without contacting a market API.
// A. Define the same four synthetic holdings.
// B. Calculate totals using decimal values and explicit rounding.
// C. Supply the CSV shown by scoped file and CodeAct tools.
using System.Globalization;

namespace MafClaw.Session04;

public static class MockPortfolio
{
    public static IReadOnlyList<Holding> Holdings { get; } = Array.AsReadOnly<Holding>(
    [
        new("MSFT", 35, 430.12m, "Technology"),
        new("NVDA", 20, 142.50m, "Technology"),
        new("JNJ", 40, 156.30m, "Healthcare"),
        new("XOM", 25, 118.75m, "Energy")
    ]);

    public static PortfolioSummary Summarize(IEnumerable<Holding>? holdings = null)
    {
        var rows = (holdings ?? Holdings).ToArray();
        if (rows.Any(row => row.Shares < 0 || row.Price < 0))
            throw new ArgumentException("Synthetic holdings cannot have negative quantities or prices.");
        var total = rows.Sum(row => row.Value);
        var technology = rows.Where(row => row.Sector == "Technology").Sum(row => row.Value);
        return new(decimal.Round(total, 2, MidpointRounding.AwayFromZero),
            total == 0 ? 0 : decimal.Round(technology / total * 100, 2, MidpointRounding.AwayFromZero),
            "Mock educational snapshot. Not financial advice.");
    }

    public static string Csv => "symbol,shares,price,sector\n" +
        string.Join('\n', Holdings.Select(row => string.Create(CultureInfo.InvariantCulture,
            $"{row.Symbol},{row.Shares},{row.Price},{row.Sector}"))) + "\n";
}
