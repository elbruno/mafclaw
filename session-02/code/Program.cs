using System.Globalization;
using System.Text.Json;

var demo = new Session02Demo();
await demo.RunAsync();

internal sealed class Session02Demo
{
    private readonly string _workingDir;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public Session02Demo()
    {
        _workingDir = Path.Combine(AppContext.BaseDirectory, "working");
        EnsureDemoFiles();
    }

    public async Task RunAsync()
    {
        Console.WriteLine("mafclaw · Session 02");
        Console.WriteLine("Working with your data safely: files, approvals and memory");
        Console.WriteLine("Mock data only. Not financial advice.");
        Console.WriteLine();

        var approvalGate = new ConsoleApprovalGate();
        var memory = new FileMemoryStore(Path.Combine(_workingDir, "memory.json"));
        var portfolioPath = Path.Combine(_workingDir, "portfolio.csv");
        var reportPath = Path.Combine(_workingDir, "reports", "portfolio-summary.md");

        Console.WriteLine("Step 1: read the portfolio from a safe working folder");
        var readApproved = await approvalGate.RequestApprovalAsync("Read local portfolio file");
        if (!readApproved)
        {
            Console.WriteLine("Read denied. The agent must stop at the approval boundary.");
            return;
        }

        var portfolio = await LoadPortfolioAsync(portfolioPath);
        var holdings = portfolio.Select(p => $"{p.Symbol}: {p.Shares} shares @ ${p.AverageCost:F2}").ToList();

        Console.WriteLine("Portfolio contents:");
        foreach (var item in holdings)
        {
            Console.WriteLine($"  - {item}");
        }

        Console.WriteLine();
        Console.WriteLine("Step 2: write a short report, but only after approval");
        var writeApproved = await approvalGate.RequestApprovalAsync("Write portfolio summary to disk");
        if (writeApproved)
        {
            var report = BuildSummaryReport(portfolio, "Conservative allocation");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            await File.WriteAllTextAsync(reportPath, report);
            Console.WriteLine($"Report saved to: {reportPath}");
        }
        else
        {
            Console.WriteLine("Write denied. No report was saved.");
        }

        Console.WriteLine();
        Console.WriteLine("Step 3: remember user preferences and watchlist updates");
        await memory.SaveAsync("user-preference", "Conservative investor; saving for a house in two years.");
        await memory.SaveAsync("watchlist", "MSFT, SPY");

        Console.WriteLine($"User preference: {await memory.GetAsync("user-preference")}");
        Console.WriteLine($"Watchlist: {await memory.GetAsync("watchlist")}");

        Console.WriteLine();
        Console.WriteLine("Step 4: approval gate before side effects");
        var tradeApproved = await approvalGate.RequestApprovalAsync("Place simulated trade: buy 10 shares of MSFT");
        if (tradeApproved)
        {
            Console.WriteLine("Trade accepted. This is a demo-only simulated order, not a real transaction.");
        }
        else
        {
            Console.WriteLine("Trade denied. Human approval is required for side effects.");
        }

        Console.WriteLine();
        Console.WriteLine("Memory persists across a simulated restart:");
        var freshStore = new FileMemoryStore(Path.Combine(_workingDir, "memory.json"));
        Console.WriteLine($"Restarted memory: {await freshStore.GetAsync("user-preference")}");
        Console.WriteLine($"Restored watchlist: {await freshStore.GetAsync("watchlist")}");
        Console.WriteLine();
        Console.WriteLine("Session 2 demo complete.");
    }

    private void EnsureDemoFiles()
    {
        Directory.CreateDirectory(_workingDir);
        var portfolioPath = Path.Combine(_workingDir, "portfolio.csv");
        var reportDir = Path.Combine(_workingDir, "reports");

        if (!File.Exists(portfolioPath))
        {
            var csv = "symbol,shares,averageCost,risk\nMSFT,35,430.12,moderate\nNVDA,20,142.50,high\nSPY,50,530.25,low\n";
            File.WriteAllText(portfolioPath, csv);
        }

        Directory.CreateDirectory(reportDir);
    }

    private static async Task<List<PortfolioHolding>> LoadPortfolioAsync(string portfolioPath)
    {
        var lines = await File.ReadAllLinesAsync(portfolioPath);
        var holdings = new List<PortfolioHolding>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
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

    private static string BuildSummaryReport(IEnumerable<PortfolioHolding> portfolio, string strategy)
    {
        var items = portfolio.ToList();
        var totalValue = items.Sum(i => i.Shares * i.AverageCost);
        var lines = new List<string>
        {
            "# Portfolio Summary",
            "",
            $"Strategy: {strategy}",
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            "",
            "| Symbol | Shares | Average Cost | Risk |",
            "| --- | ---: | ---: | --- |"
        };

        foreach (var item in items)
        {
            lines.Add($"| {item.Symbol} | {item.Shares} | ${item.AverageCost:F2} | {item.Risk} |");
        }

        lines.Add("");
        lines.Add($"**Estimated portfolio value:** ${totalValue:F2}");
        lines.Add("\nThis summary is mock educational content, not financial advice.");

        return string.Join(Environment.NewLine, lines);
    }
}

internal sealed record PortfolioHolding(string Symbol, int Shares, decimal AverageCost, string Risk);

internal sealed class ConsoleApprovalGate
{
    public async Task<bool> RequestApprovalAsync(string action)
    {
        Console.Write($"Approve this action: {action}? [y/N]: ");
        var response = await Task.Run(() => Console.ReadLine());
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class FileMemoryStore
{
    private readonly string _memoryPath;
    private readonly Dictionary<string, string> _entries;

    public FileMemoryStore(string memoryPath)
    {
        _memoryPath = memoryPath;
        Directory.CreateDirectory(Path.GetDirectoryName(memoryPath)!);
        _entries = File.Exists(memoryPath)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(memoryPath)) ?? new Dictionary<string, string>()
            : new Dictionary<string, string>();
    }

    public async Task SaveAsync(string key, string value)
    {
        _entries[key] = value;
        await File.WriteAllTextAsync(_memoryPath, JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true }));
    }

    public Task<string?> GetAsync(string key)
    {
        return Task.FromResult(_entries.TryGetValue(key, out var value) ? value : null);
    }
}
