using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace MafClaw.Checkpoint04;

internal sealed class StockTools
{
    private static readonly Regex SymbolPattern =
        new("^[A-Z]{1,5}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IReadOnlyDictionary<string, StockQuote> _quotes;

    public StockTools(string marketDataPath)
    {
        using var stream = File.OpenRead(marketDataPath);
        var rows = JsonSerializer.Deserialize<List<StockQuoteRow>>(
                       stream,
                       new JsonSerializerOptions(JsonSerializerDefaults.Web))
                   ?? throw new InvalidOperationException(
                       "The mock market data fixture is empty.");

        _quotes = rows.ToDictionary(
            row => NormalizeSymbol(row.Symbol),
            row => new StockQuote(
                NormalizeSymbol(row.Symbol),
                row.Price,
                string.IsNullOrWhiteSpace(row.Currency)
                    ? "USD"
                    : row.Currency.Trim().ToUpperInvariant(),
                string.IsNullOrWhiteSpace(row.Source)
                    ? "mock"
                    : row.Source.Trim()),
            StringComparer.OrdinalIgnoreCase);
    }

    [Description("Gets the latest illustrative stock price for a ticker symbol.")]
    public StockQuote GetStockPrice(
        [Description("Stock ticker symbol, for example MSFT or NVDA.")] string symbol)
    {
        var normalized = NormalizeSymbol(symbol);
        return _quotes.TryGetValue(normalized, out var quote)
            ? quote
            : throw new KeyNotFoundException(
                "No illustrative quote is available for that symbol.");
    }

    public AIFunction CreateGetStockPriceTool() =>
        AIFunctionFactory.Create(GetStockPrice, "get_stock_price");

    private static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("A stock symbol is required.", nameof(symbol));
        }

        var normalized = symbol.Trim().ToUpperInvariant();
        if (!SymbolPattern.IsMatch(normalized))
        {
            throw new ArgumentException(
                "The stock symbol must contain one to five letters.",
                nameof(symbol));
        }

        return normalized;
    }

    private sealed record StockQuoteRow(
        string Symbol,
        decimal Price,
        string Currency,
        string Source);
}

internal sealed record StockQuote(
    string Symbol,
    decimal Price,
    string Currency,
    string Source);
