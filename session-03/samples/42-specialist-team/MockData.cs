// Objective: expose only deterministic, read-only educational data tools.
// A. Load fixed local holdings and dated fictional news.
// B. Calculate allocation and a transparent stress scenario with decimal math.
// C. Return advisory evidence; never accept paths or trade instructions.

using System.ComponentModel;
using System.Text.Json;

namespace MafClaw.Sample42;

public sealed class MockData
{
    private readonly Holding[] _holdings;
    private readonly string _news;

    public MockData()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Fixtures42");
        _holdings = JsonSerializer.Deserialize<Holding[]>(
            File.ReadAllText(Path.Combine(directory, "holdings.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Mock holdings could not be loaded.");
        _news = File.ReadAllText(Path.Combine(directory, "news.json"));
        if (_holdings.Length == 0 || _holdings.Any(item => item.Units < 0 || item.Price <= 0) ||
            _holdings.Sum(item => item.Value) <= 0)
        {
            throw new InvalidDataException("Mock holdings are invalid.");
        }
    }

    [Description("Read dated FICTIONAL educational news. Not current market information. No network or writes.")]
    public string ReadNews() => _news;

    [Description("Calculate asset-class weights from fixed MOCK holdings using decimal arithmetic. Read-only; no trades.")]
    public string CalculateAllocation()
    {
        decimal total = _holdings.Sum(item => item.Value);
        return JsonSerializer.Serialize(new
        {
            label = "EDUCATIONAL MOCK HOLDINGS - not financial advice",
            totalValue = total,
            allocations = _holdings.GroupBy(item => item.AssetClass)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new
                {
                    assetClass = group.Key,
                    value = group.Sum(item => item.Value),
                    percent = group.Sum(item => item.Value) / total * 100m
                })
        });
    }

    [Description("Assess MOCK concentration and stress loss: equities -20%, bonds -5%, cash 0%. Read-only, not a forecast.")]
    public string AssessRisk()
    {
        decimal total = _holdings.Sum(item => item.Value);
        Holding largest = _holdings.MaxBy(item => item.Value)!;
        decimal stressLoss = _holdings.Sum(item => item.Value * (item.AssetClass switch
        {
            "Equities" => 0.20m,
            "Bonds" => 0.05m,
            "Cash" => 0m,
            _ => throw new InvalidDataException("Unknown mock asset class.")
        }));
        return JsonSerializer.Serialize(new
        {
            label = "EDUCATIONAL MOCK HOLDINGS - not financial advice",
            largestHolding = largest.Symbol,
            largestHoldingPercent = largest.Value / total * 100m,
            concentrationThresholdPercent = 40m,
            concentrationFlag = largest.Value / total > 0.40m,
            stressLoss,
            stressLossPercent = stressLoss / total * 100m,
            scenario = "Equities -20%, bonds -5%, cash 0%; illustrative, not a forecast."
        });
    }
}
