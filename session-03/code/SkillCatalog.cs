// Objective: load the catalog that advertises the sample's local skills.
// Steps:
// A. Resolve the checked-in catalog file.
// B. Deserialize skill names and purposes.
// C. Return the catalog for the host-owned workflow.

using System.Text.Json;

internal sealed class SkillCatalog
{
    public IReadOnlyList<SkillDefinition> Skills { get; init; } = [];

    public static SkillCatalog Load()
    {
        var path = ResolvePath("skill-catalog.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SkillCatalog>(
                   json,
                   new JsonSerializerOptions(JsonSerializerDefaults.Web))
               ?? throw new InvalidOperationException("Unable to load skill-catalog.json");
    }

    private static string ResolvePath(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, fileName);
        return File.Exists(outputPath)
            ? outputPath
            : Path.Combine(Directory.GetCurrentDirectory(), fileName);
    }
}

internal sealed record SkillDefinition(string Name, string Purpose);
