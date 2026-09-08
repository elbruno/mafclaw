using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

internal static class MemoryStoreTools
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static string memoryPath = string.Empty;

    public static void Initialize(string path)
    {
        memoryPath = path;
        Directory.CreateDirectory(Path.GetDirectoryName(memoryPath)!);
    }

    [Description("Stores a durable user preference in local file memory for this demo.")]
    public static string RememberUserPreferenceValue(
        [Description("Preference or user fact to remember.")] string preference)
    {
        var memory = LoadMemory();
        memory["user-preference"] = preference;
        SaveMemory(memory);
        return "Remembered user-preference in local file memory.";
    }

    [Description("Gets a value from local file memory by key, such as user-preference or watchlist.")]
    public static string GetMemoryValue(
        [Description("Memory key to read.")] string key)
    {
        var memory = LoadMemory();
        return memory.TryGetValue(key, out var value)
            ? value
            : $"No memory found for key '{key}'.";
    }

    public static AIFunction RememberUserPreference { get; } =
        AIFunctionFactory.Create(RememberUserPreferenceValue, "remember_user_preference");

    public static AIFunction GetMemory { get; } =
        AIFunctionFactory.Create(GetMemoryValue, "get_memory");

    private static Dictionary<string, string> LoadMemory()
    {
        if (!File.Exists(memoryPath))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(memoryPath)) ?? new Dictionary<string, string>();
    }

    private static void SaveMemory(Dictionary<string, string> memory)
    {
        File.WriteAllText(memoryPath, JsonSerializer.Serialize(memory, JsonOptions));
    }
}
