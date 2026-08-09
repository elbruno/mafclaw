using System.Text.Json;

var sample = new Session04Sample(Session04Settings.Load(), TelemetryTemplate.Load(), new GovernanceChecklist());

Console.WriteLine("mafclaw · Session 04 snapshot");
Console.WriteLine("Illustrative sample only. Mock data only. Not financial advice.");
Console.WriteLine();

await sample.RunAsync();

internal sealed class Session04Sample(
    Session04Settings settings,
    TelemetryTemplate telemetry,
    GovernanceChecklist governanceChecklist)
{
    private readonly Session04Settings _settings = settings;
    private readonly TelemetryTemplate _telemetry = telemetry;
    private readonly GovernanceChecklist _governanceChecklist = governanceChecklist;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public Task RunAsync()
    {
        Console.WriteLine("Planned steps");
        foreach (var step in new[]
        {
            "Load placeholder telemetry settings",
            "Evaluate governance checks",
            "Render a deployment-ready summary"
        })
        {
            Console.WriteLine($"- {step}");
        }

        Console.WriteLine();

        var result = new
        {
            session = "04",
            featureSet = new[] { "observability", "governance", "deployment" },
            model = _settings.Models.PrimaryModel,
            telemetry = _telemetry,
            governance = _governanceChecklist.Items,
            deployment = new
            {
                _settings.Deployment.EnvironmentName,
                _settings.Deployment.ContainerImage,
                note = "All deployment values are placeholders and safe for source control."
            }
        };

        Console.WriteLine("Sample response");
        Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        return Task.CompletedTask;
    }
}

internal sealed class Session04Settings
{
    public ModelSettings Models { get; init; } = new();
    public DeploymentSettings Deployment { get; init; } = new();

    public static Session04Settings Load()
    {
        var path = ResolvePath("appsettings.template.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Session04Settings>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
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

internal sealed class DeploymentSettings
{
    public string EnvironmentName { get; init; } = "demo-staging";
    public string ContainerImage { get; init; } = "ghcr.io/example/mafclaw:session-04-placeholder";
}

internal sealed class TelemetryTemplate
{
    public string ServiceName { get; init; } = "mafclaw-session-04";
    public string ExporterEndpoint { get; init; } = "https://example.invalid/otlp";
    public string DashboardHint { get; init; } = "Use local Aspire or OpenTelemetry dashboards during demos.";

    public static TelemetryTemplate Load()
    {
        var path = ResolvePath("telemetry-template.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TelemetryTemplate>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to load telemetry-template.json");
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

internal sealed class GovernanceChecklist
{
    public IReadOnlyList<string> Items { get; } =
    [
        "No real secrets in config or source.",
        "Mock-only claims remain visible in docs and console output.",
        "Observability endpoints are placeholders until local override.",
        "Deployment guidance stays reproducible for attendees."
    ];
}
