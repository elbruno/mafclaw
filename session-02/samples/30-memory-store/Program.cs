using System.Text.Json;

var memoryPath = Path.Combine(AppContext.BaseDirectory, "memory.json");
var memory = Load(memoryPath);
memory["user-preference"] = "Conservative investor for a house purchase in two years.";
memory["watchlist"] = "MSFT, SPY";
Save(memoryPath, memory);

Console.WriteLine("Session 2 sample 30: memory store");
Console.WriteLine("Saved state before restart:");
Console.WriteLine($"user-preference = {memory["user-preference"]}");
Console.WriteLine($"watchlist = {memory["watchlist"]}");

var reloaded = Load(memoryPath);
Console.WriteLine();
Console.WriteLine("State after simulated restart:");
Console.WriteLine($"user-preference = {reloaded["user-preference"]}");
Console.WriteLine($"watchlist = {reloaded["watchlist"]}");

static Dictionary<string, string> Load(string path)
{
    if (!File.Exists(path))
    {
        return new Dictionary<string, string>();
    }

    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
}

static void Save(string path, Dictionary<string, string> memory)
{
    File.WriteAllText(path, JsonSerializer.Serialize(memory, new JsonSerializerOptions { WriteIndented = true }));
}
