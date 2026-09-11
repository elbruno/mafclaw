using System.Text.Json;

internal sealed class Session03Settings
{
    public ShellSettings Shell { get; init; } = new();
    public BackgroundSettings Background { get; init; } = new();

    public static Session03Settings Load()
    {
        var path = ResolvePath("appsettings.template.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Session03Settings>(
                   json,
                   new JsonSerializerOptions(JsonSerializerDefaults.Web))
               ?? throw new InvalidOperationException("Unable to load appsettings.template.json");
    }

    private static string ResolvePath(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, fileName);
        return File.Exists(outputPath)
            ? outputPath
            : Path.Combine(Directory.GetCurrentDirectory(), fileName);
    }
}

internal sealed class ShellSettings
{
    public string Command { get; init; } = "dotnet --version";
    public int TimeoutSeconds { get; init; } = 5;
    public int MaxOutputCharacters { get; init; } = 2_000;
}

internal sealed class BackgroundSettings
{
    public int SimulatedDurationMilliseconds { get; init; } = 1_000;
}
