// Session flow:
// A. Load the small JSON memory file.
// B. Save a user preference and watchlist.
// C. Print the state before the simulated restart.
// D. Reload the file and prove the state survived.

using System.Text.Json;

// Store memory next to the sample output so it survives a simulated restart.
var memoryPath = Path.Combine(AppContext.BaseDirectory, "memory.json");

// Load existing memory, then add the facts this sample wants to preserve.
var memory = Load(memoryPath);
memory["user-preference"] = "Conservative investor for a house purchase in two years.";
memory["watchlist"] = "MSFT, SPY";
Save(memoryPath, memory);

Console.WriteLine("Session 2 sample 30: memory store");
Console.WriteLine("Saved state before restart:");
Console.WriteLine($"user-preference = {memory["user-preference"]}");
Console.WriteLine($"watchlist = {memory["watchlist"]}");

// Reload from disk to prove the values were persisted.
var reloaded = Load(memoryPath);
Console.WriteLine();
Console.WriteLine("State after simulated restart:");
Console.WriteLine($"user-preference = {reloaded["user-preference"]}");
Console.WriteLine($"watchlist = {reloaded["watchlist"]}");

static Dictionary<string, string> Load(string path)
{
    // First run starts with empty memory.
    if (!File.Exists(path))
    {
        return new Dictionary<string, string>();
    }

    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
}

static void Save(string path, Dictionary<string, string> memory)
{
    // Indented JSON keeps the sample state easy to inspect on screen.
    File.WriteAllText(path, JsonSerializer.Serialize(memory, new JsonSerializerOptions { WriteIndented = true }));
}
