using System.Globalization;

var skillsDirectory = Path.Combine(AppContext.BaseDirectory, "skills");
var skills = DiscoverSkills(skillsDirectory);

Console.WriteLine("Sample 10 - File-based skills in plain C#");
Console.WriteLine("A skill packages instructions and resources. It is not a dictionary entry.");
Console.WriteLine();

Console.WriteLine("1. Advertise only the skill names and descriptions:");
foreach (var skill in skills)
{
    Console.WriteLine($"   - {skill.Name}: {skill.Description}");
}

var request = "Value MSFT for my mock portfolio.";
var selectedSkill = skills.Single(skill => skill.Name == "valuation");

Console.WriteLine();
Console.WriteLine($"2. Request: {request}");
Console.WriteLine($"3. Selected skill: {selectedSkill.Name}");
Console.WriteLine("4. Load its instructions and resource:");
Console.WriteLine(Indent(selectedSkill.Instructions, "   "));

var prices = LoadPrices(selectedSkill.Directory);
var result = ValueHolding("MSFT", 25, prices);
Console.WriteLine($"5. Host-owned execution: 25 MSFT x ${result.Price.ToString("0.00", CultureInfo.InvariantCulture)} = ${result.Value.ToString("0.00", CultureInfo.InvariantCulture)}");
Console.WriteLine("   Mock educational data only. This is not financial advice.");

static IReadOnlyList<FileSkill> DiscoverSkills(string skillsDirectory)
{
    if (!Directory.Exists(skillsDirectory))
    {
        throw new DirectoryNotFoundException($"Skills directory not found: {skillsDirectory}");
    }

    return Directory
        .EnumerateFiles(skillsDirectory, "SKILL.md", SearchOption.AllDirectories)
        .Select(ParseSkill)
        .OrderBy(skill => skill.Name, StringComparer.Ordinal)
        .ToList();
}

static FileSkill ParseSkill(string skillPath)
{
    var content = File.ReadAllText(skillPath).Replace("\r\n", "\n", StringComparison.Ordinal);
    var parts = content.Split("---\n", StringSplitOptions.None);
    if (parts.Length < 3)
    {
        throw new InvalidOperationException($"Skill front matter is invalid: {skillPath}");
    }

    var frontMatter = parts[1]
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Split(':', 2, StringSplitOptions.TrimEntries))
        .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

    return new FileSkill(
        frontMatter["name"],
        frontMatter["description"],
        parts[2].Trim(),
        Path.GetDirectoryName(skillPath) ?? throw new InvalidOperationException("Skill directory is unavailable."));
}

static IReadOnlyDictionary<string, decimal> LoadPrices(string skillDirectory)
{
    var pricePath = Path.Combine(skillDirectory, "references", "mock-prices.csv");
    return File.ReadLines(pricePath)
        .Skip(1)
        .Select(line => line.Split(',', 2, StringSplitOptions.TrimEntries))
        .ToDictionary(
            fields => fields[0],
            fields => decimal.Parse(fields[1], CultureInfo.InvariantCulture),
            StringComparer.OrdinalIgnoreCase);
}

static HoldingValue ValueHolding(string symbol, int shares, IReadOnlyDictionary<string, decimal> prices)
{
    if (!prices.TryGetValue(symbol, out var price))
    {
        throw new InvalidOperationException($"The valuation skill has no mock price for {symbol}.");
    }

    return new HoldingValue(price, shares * price);
}

static string Indent(string text, string prefix) =>
    string.Join(Environment.NewLine, text.Split('\n').Select(line => prefix + line));

internal sealed record FileSkill(string Name, string Description, string Instructions, string Directory);
internal sealed record HoldingValue(decimal Price, decimal Value);
