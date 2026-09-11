using System.Text.Json;

internal sealed class MockWatchlist(IReadOnlyList<WatchlistItem> items)
{
    public static MockWatchlist Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "mock-watchlist.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "mock-watchlist.json");
        }

        var json = File.ReadAllText(path);
        var items = JsonSerializer.Deserialize<List<WatchlistItem>>(
                        json,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    ?? throw new InvalidOperationException("Unable to load mock-watchlist.json");
        return new MockWatchlist(items);
    }

    public PortfolioSummary Summarize()
    {
        var averageChange = items.Count == 0 ? 0 : (double)items.Average(item => item.ChangePercent);
        return new PortfolioSummary(items.Count, items.Select(item => item.Symbol).ToArray(), averageChange);
    }
}

internal sealed record WatchlistItem(string Symbol, decimal ChangePercent);

internal sealed record PortfolioSummary(
    int ItemCount,
    IReadOnlyList<string> Symbols,
    double AverageChangePercent);
