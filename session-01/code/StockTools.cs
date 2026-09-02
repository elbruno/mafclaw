using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace MafClaw.Session01;

internal sealed class StockTools
{
    private static readonly Regex SymbolPattern =
        new("^[A-Z]{1,5}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IReadOnlyDictionary<string, StockQuote> _quotes;

    public StockTools(string marketDataPath)
    {
        _quotes = LoadQuotes(marketDataPath);
    }

    [Description("Gets the latest illustrative stock price for a ticker symbol.")]
    public StockQuote GetStockPrice(
        [Description("Stock ticker symbol, for example MSFT or NVDA.")] string symbol)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        if (!_quotes.TryGetValue(normalizedSymbol, out var quote))
        {
            throw new KeyNotFoundException($"No mock quote found for symbol '{normalizedSymbol}'.");
        }

        return quote;
    }

    public AIFunction CreateGetStockPriceTool()
    {
        return AIFunctionFactory.Create(GetStockPrice, "get_stock_price");
    }

    internal static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Stock symbol is required.", nameof(symbol));
        }

        var normalized = symbol.Trim().ToUpperInvariant();
        if (!SymbolPattern.IsMatch(normalized))
        {
            throw new ArgumentException(
                "Stock symbol must contain only letters and be 1 to 5 characters long.",
                nameof(symbol));
        }

        return normalized;
    }

    private static IReadOnlyDictionary<string, StockQuote> LoadQuotes(string marketDataPath)
    {
        if (!File.Exists(marketDataPath))
        {
            throw new FileNotFoundException("Market data file was not found.", marketDataPath);
        }

        using var stream = File.OpenRead(marketDataPath);
        var rows = JsonSerializer.Deserialize<List<StockQuoteRow>>(
                       stream,
                       new JsonSerializerOptions(JsonSerializerDefaults.Web))
                   ?? throw new InvalidOperationException("Market data file is empty.");

        return rows.ToDictionary(
            row => NormalizeSymbol(row.Symbol),
            row => new StockQuote(
                NormalizeSymbol(row.Symbol),
                row.Price,
                string.IsNullOrWhiteSpace(row.Currency) ? "USD" : row.Currency.Trim().ToUpperInvariant(),
                string.IsNullOrWhiteSpace(row.Source) ? "mock" : row.Source.Trim()),
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed record StockQuoteRow(string Symbol, decimal Price, string Currency, string Source);
}

internal sealed record StockQuote(string Symbol, decimal Price, string Currency, string Source);
