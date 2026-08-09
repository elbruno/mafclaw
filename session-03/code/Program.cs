using System.Text.Json;

var sample = new Session03Sample(Session03Settings.Load(), SkillCatalog.Load(), new PlaceholderShell(), new BackgroundTaskRunner());

Console.WriteLine("mafclaw · Session 03 snapshot");
Console.WriteLine("Illustrative sample only. Mock data only. Not financial advice.");
Console.WriteLine();

await sample.RunAsync();

internal sealed class Session03Sample(
    Session03Settings settings,
    SkillCatalog skillCatalog,
    IShellTool shellTool,
    IBackgroundTaskRunner backgroundTaskRunner)
{
    private readonly Session03Settings _settings = settings;
    private readonly SkillCatalog _skillCatalog = skillCatalog;
    private readonly IShellTool _shellTool = shellTool;
    private readonly IBackgroundTaskRunner _backgroundTaskRunner = backgroundTaskRunner;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task RunAsync()
    {
        Console.WriteLine("Planned steps");
        foreach (var step in new[]
        {
            "Load a local skill catalog",
            "Run a safe placeholder shell command",
            "Queue a background research task",
            "Return a compact orchestration summary"
        })
        {
            Console.WriteLine($"- {step}");
        }

        Console.WriteLine();

        var shellResult = await _shellTool.RunAsync(_settings.Shell.PlaceholderCommand);
        var backgroundTask = await _backgroundTaskRunner.QueueAsync("summarize-volatility", "Collect a mock market volatility note in the background.");

        var result = new
        {
            session = "03",
            featureSet = new[] { "skills", "shell", "codeact", "background_agents" },
            skills = _skillCatalog.Skills,
            shell = shellResult,
            backgroundTask,
            guidance = new[]
            {
                "Shell access is placeholder-only in this snapshot.",
                "Background work returns a fake ticket instead of starting real processes.",
                "Keep demo skills explicit and readable on stream."
            }
        };

        Console.WriteLine("Sample response");
        Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
    }
}

internal interface IShellTool
{
    Task<ShellResult> RunAsync(string command);
}

internal sealed class PlaceholderShell : IShellTool
{
    public Task<ShellResult> RunAsync(string command)
    {
        return Task.FromResult(new ShellResult(command, 0, "Placeholder shell output: workspace scan completed with mock results."));
    }
}

internal sealed record ShellResult(string Command, int ExitCode, string Output);

internal interface IBackgroundTaskRunner
{
    Task<BackgroundTaskTicket> QueueAsync(string name, string description);
}

internal sealed class BackgroundTaskRunner : IBackgroundTaskRunner
{
    public Task<BackgroundTaskTicket> QueueAsync(string name, string description)
    {
        var ticket = new BackgroundTaskTicket(name, description, $"bg-{Guid.NewGuid():N}"[..10], "queued-placeholder");
        return Task.FromResult(ticket);
    }
}

internal sealed record BackgroundTaskTicket(string Name, string Description, string TicketId, string Status);

internal sealed class SkillCatalog
{
    public IReadOnlyList<SkillDefinition> Skills { get; init; } = [];

    public static SkillCatalog Load()
    {
        var path = ResolvePath("skill-catalog.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SkillCatalog>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to load skill-catalog.json");
    }

    private static string ResolvePath(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), fileName);
    }
}

internal sealed record SkillDefinition(string Name, string Purpose);

internal sealed class Session03Settings
{
    public ModelSettings Models { get; init; } = new();
    public ShellSettings Shell { get; init; } = new();

    public static Session03Settings Load()
    {
        var path = ResolvePath("appsettings.template.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Session03Settings>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to load appsettings.template.json");
    }

    private static string ResolvePath(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), fileName);
    }
}

internal sealed class ModelSettings
{
    public string PrimaryModel { get; init; } = "placeholder-model";
}

internal sealed class ShellSettings
{
    public string PlaceholderCommand { get; init; } = "dotnet --info";
}
