using System.ComponentModel;
using System.Globalization;
using Microsoft.Extensions.AI;

internal static class SafeFileAccessTools
{
    private static string workingDirectory = string.Empty;

    public static void Initialize(string root)
    {
        workingDirectory = root;
        Directory.CreateDirectory(workingDirectory);

        var portfolioPath = Path.Combine(workingDirectory, "portfolio.csv");
        if (!File.Exists(portfolioPath))
        {
            File.WriteAllText(
                portfolioPath,
                "symbol,shares,averageCost,risk\nMSFT,25,430.10,moderate\nSPY,40,530.25,low\nNVDA,18,142.50,high\n");
        }
    }

    [Description("Reads the mock portfolio from the approved working folder and returns a concise summary.")]
    public static string ReadPortfolioSummaryFromWorkingFolder()
    {
        var portfolioPath = Path.Combine(workingDirectory, "portfolio.csv");
        if (!IsSafePath(portfolioPath))
        {
            return "Blocked: the requested path is outside the approved working folder.";
        }

        var lines = File.ReadAllLines(portfolioPath);
        var summaryLines = new List<string>();
        decimal totalValue = 0;

        for (var i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 4)
            {
                continue;
            }

            var shares = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var averageCost = decimal.Parse(parts[2], CultureInfo.InvariantCulture);
            totalValue += shares * averageCost;
            summaryLines.Add($"{parts[0]}: {shares} shares @ ${averageCost:F2} ({parts[3]} risk)");
        }

        summaryLines.Add($"Estimated mock value: ${totalValue:F2}");
        return string.Join(Environment.NewLine, summaryLines);
    }

    public static AIFunction ReadPortfolioSummary { get; } =
        AIFunctionFactory.Create(ReadPortfolioSummaryFromWorkingFolder, "read_portfolio_summary");

    private static bool IsSafePath(string candidatePath)
    {
        var fullCandidate = Path.GetFullPath(candidatePath);
        var fullRoot = Path.GetFullPath(workingDirectory);
        return fullCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }
}
