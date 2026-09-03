using System.ComponentModel;
using Microsoft.Extensions.AI;

static class StockTools
{
    static readonly Dictionary<string, decimal> Prices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MSFT"] = 512.34m,
        ["NVDA"] = 184.72m,
        ["AMZN"] = 241.18m,
    };

    [Description("Gets the illustrative stock price for a ticker symbol.")]
    public static string GetStockPriceBySymbol(
        [Description("Stock ticker symbol, e.g. MSFT")] string symbol)
    {
        var upper = symbol.Trim().ToUpperInvariant();
        return Prices.TryGetValue(upper, out var price)
            ? $"{upper}: {price:F2} USD (mock)"
            : $"{upper}: not available";
    }

    public static AIFunction GetStockPrice { get; } =
        AIFunctionFactory.Create(GetStockPriceBySymbol, "get_stock_price");
}
