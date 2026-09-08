using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.AI;

internal static class AgentFinanceTools
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static string workingDirectory = string.Empty;
    private static string memoryPath = string.Empty;

    public static void Initialize(string root)
    {
        workingDirectory = root;
        memoryPath = Path.Combine(workingDirectory, "memory.json");
        Directory.CreateDirectory(workingDirectory);
        Directory.CreateDirectory(Path.Combine(workingDirectory, "reports"));

        var portfolioPath = Path.Combine(workingDirectory, "portfolio.csv");
        if (!File.Exists(portfolioPath))
        {
            File.WriteAllText(
                portfolioPath,
                "symbol,shares,averageCost,risk\nMSFT,35,430.12,moderate\nNVDA,20,142.50,high\nSPY,50,530.25,low\n");
        }
    }

    [Description("Reads the mock portfolio from the approved working folder and returns a concise summary.")]
    public static string ReadPortfolioSummaryFromWorkingFolder()
    {
        var holdings = LoadPortfolio();
        var totalValue = holdings.Sum(item => item.Shares * item.AverageCost);
        var lines = holdings.Select(item => $"{item.Symbol}: {item.Shares} shares @ ${item.AverageCost:F2} ({item.Risk} risk)");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine + $"Estimated mock value: ${totalValue:F2}";
    }

    [Description("Writes a markdown portfolio report inside the approved reports folder after human approval.")]
    public static string WritePortfolioReportToWorkingFolder(
        [Description("Short markdown report body to save.")] string markdownReport)
    {
        if (!Confirm("Approve writing portfolio-summary.md inside the approved reports folder?"))
        {
            return "Denied: report was not written.";
        }

        var reportPath = Path.Combine(workingDirectory, "reports", "portfolio-summary.md");
        File.WriteAllText(reportPath, markdownReport);
        return $"Approved: report written to {reportPath}";
    }

    [Description("Stores a durable user preference in local file memory for this demo.")]
    public static string RememberUserPreferenceValue(
        [Description("Preference or user fact to remember.")] string preference)
    {
        var memory = LoadMemory();
        memory["user-preference"] = preference;
        SaveMemory(memory);
        return "Remembered user-preference in file memory.";
    }

    [Description("Gets a value from local file memory by key, such as user-preference or watchlist.")]
    public static string GetMemoryValue(
        [Description("Memory key to read.")] string key)
    {
        var memory = LoadMemory();
        return memory.TryGetValue(key, out var value)
            ? value
            : $"No memory found for key '{key}'.";
    }

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

    public static AIFunction ReadPortfolioSummary { get; } =
        AIFunctionFactory.Create(ReadPortfolioSummaryFromWorkingFolder, "read_portfolio_summary");

    public static AIFunction WritePortfolioReport { get; } =
        AIFunctionFactory.Create(WritePortfolioReportToWorkingFolder, "write_portfolio_report");

    public static AIFunction RememberUserPreference { get; } =
        AIFunctionFactory.Create(RememberUserPreferenceValue, "remember_user_preference");

    public static AIFunction GetMemory { get; } =
        AIFunctionFactory.Create(GetMemoryValue, "get_memory");

    public static AIFunction RequestSimulatedTrade { get; } =
        AIFunctionFactory.Create(RequestSimulatedTradeOrder, "request_simulated_trade");

    private static List<PortfolioHolding> LoadPortfolio()
    {
        var portfolioPath = Path.Combine(workingDirectory, "portfolio.csv");
        var lines = File.ReadAllLines(portfolioPath);
        var holdings = new List<PortfolioHolding>();

        for (var i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 4)
            {
                continue;
            }

            holdings.Add(new PortfolioHolding(
                parts[0],
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                decimal.Parse(parts[2], CultureInfo.InvariantCulture),
                parts[3]));
        }

        return holdings;
    }

    private static Dictionary<string, string> LoadMemory()
    {
        if (!File.Exists(memoryPath))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(memoryPath)) ?? new Dictionary<string, string>();
    }

    private static void SaveMemory(Dictionary<string, string> memory)
    {
        File.WriteAllText(memoryPath, JsonSerializer.Serialize(memory, JsonOptions));
    }

    private static bool Confirm(string prompt)
    {
        Console.Write($"{prompt} [y/N]: ");
        var response = Console.ReadLine();
        return response is not null &&
            (response.Equals("y", StringComparison.OrdinalIgnoreCase) ||
             response.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }
}
