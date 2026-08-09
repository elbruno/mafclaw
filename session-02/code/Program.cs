using System.Text.Json;

var settings = Session02Settings.Load();
var sample = new Session02Sample(
    ResolvePath(settings.Sample.MarketDataFile),
    settings,
    new ApprovalConsole(),
    new InMemoryMemoryStore());

Console.WriteLine("mafclaw · Session 02 snapshot");
Console.WriteLine("Illustrative sample only. Mock data only. Not financial advice.");
Console.WriteLine();

await sample.RunAsync();

static string ResolvePath(string fileName)
{
    var outputPath = Path.Combine(AppContext.BaseDirectory, fileName);
    if (File.Exists(outputPath))
    {
        return outputPath;
    }

    return Path.Combine(Directory.GetCurrentDirectory(), fileName);
}

internal sealed class Session02Sample(
    string marketDataPath,
    Session02Settings settings,
    IApprovalGate approvalGate,
    IMemoryStore memoryStore)
{
    private readonly string _marketDataPath = marketDataPath;
    private readonly Session02Settings _settings = settings;
    private readonly IApprovalGate _approvalGate = approvalGate;
    private readonly IMemoryStore _memoryStore = memoryStore;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task RunAsync()
    {
        var request = new PortfolioRequest("Bruno-demo", new[] { "MSFT", "NVDA" }, "portfolio-notes.txt");
        Console.WriteLine($"Request: summarize mock positions for {request.ProfileId}");
        Console.WriteLine();

        Console.WriteLine("Planned steps");
        foreach (var step in new[]
        {
            "Read approved local file context",
            "Load mock stock prices",
            "Store a lightweight memory entry",
            "Return a short safe summary"
        })
        {
            Console.WriteLine($"- {step}");
        }

        Console.WriteLine();

        var filePreview = await ReadApprovedFileAsync(request.ContextFile);
        var quotes = await LoadQuotesAsync(request.Symbols);
        await _memoryStore.SaveAsync($"last-profile:{request.ProfileId}", $"Viewed {string.Join(", ", request.Symbols)} from mock data.");

        var result = new
        {
            session = "02",
            featureSet = new[] { "file_access", "approvals", "memory" },
            settings = new
            {
                _settings.Models.PrimaryModel,
                _settings.Services.SearchEndpoint
            },
            filePreview,
            quotes,
            memory = await _memoryStore.GetAsync($"last-profile:{request.ProfileId}"),
            guidance = new[]
            {
                "Use approvals before reading attendee-local files.",
                "Persist only safe demo memory, never secrets.",
                "Keep all outputs mock and clearly labeled."
            }
        };

        Console.WriteLine("Sample response");
        Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
    }

    private async Task<string> ReadApprovedFileAsync(string fileName)
    {
        var approved = await _approvalGate.RequestApprovalAsync($"Read local file '{fileName}'");
        if (!approved)
        {
            return "File read skipped because approval was not granted.";
        }

        return "Approved placeholder file preview: portfolio notes mention a cautious demo posture and mock-only claims.";
    }

    private async Task<IReadOnlyList<StockQuote>> LoadQuotesAsync(IReadOnlyList<string> symbols)
    {
        await using var stream = File.OpenRead(_marketDataPath);
        var quotes = await JsonSerializer.DeserializeAsync<List<StockQuote>>(stream, _jsonOptions)
            ?? throw new InvalidOperationException("Mock market data is missing.");

        return quotes.Where(q => symbols.Contains(q.Symbol, StringComparer.OrdinalIgnoreCase)).ToList();
    }
}

internal sealed record PortfolioRequest(string ProfileId, IReadOnlyList<string> Symbols, string ContextFile);
internal sealed record StockQuote(string Symbol, decimal Price, string Currency, string Source);

internal interface IApprovalGate
{
    Task<bool> RequestApprovalAsync(string action);
}

internal sealed class ApprovalConsole : IApprovalGate
{
    public Task<bool> RequestApprovalAsync(string action)
    {
        Console.WriteLine($"Approval gate: auto-approved placeholder for '{action}'.");
        return Task.FromResult(true);
    }
}

internal interface IMemoryStore
{
    Task SaveAsync(string key, string value);
    Task<string?> GetAsync(string key);
}

internal sealed class InMemoryMemoryStore : IMemoryStore
{
    private readonly Dictionary<string, string> _entries = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(string key, string value)
    {
        _entries[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        _entries.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }
}

internal sealed class Session02Settings
{
    public ModelSettings Models { get; init; } = new();
    public ServiceSettings Services { get; init; } = new();
    public SampleSettings Sample { get; init; } = new();

    public static Session02Settings Load()
    {
        var path = ResolveSettingsPath();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Session02Settings>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to load appsettings.template.json");
    }

    private static string ResolveSettingsPath()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "appsettings.template.json");
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "appsettings.template.json");
    }
}

internal sealed class ModelSettings
{
    public string PrimaryModel { get; init; } = "placeholder-model";
}

internal sealed class ServiceSettings
{
    public string SearchEndpoint { get; init; } = "https://example.invalid/search";
    public string SearchApiKey { get; init; } = "set-via-user-secrets-or-env";
}

internal sealed class SampleSettings
{
    public string MarketDataFile { get; init; } = "mock-market-data.json";
}
