// Objective: Sample 60 (plain C#) saves and restores a transcript in one dotnet run.
// A. Keep a serializable list of turns instead of relying on model memory.
// B. Save two exchanges to JSON.
// C. Reload the file, add another exchange and prove the count continues.

using System.Text.Json;
using MafClaw.Session04.Samples;

try
{
    if (args.Length > 1 || (args.Length == 1 && args[0] != "--self-test"))
        throw new InvalidOperationException("Run with dotnet run. Automated check: --self-test.");
    var selfTest = args.Contains("--self-test");

    // A. The transcript is ordinary application-owned state, with no model or framework.
    List<Turn> transcript = [];
    var folder = selfTest ? Path.GetTempPath() : Path.Combine(Environment.CurrentDirectory, ".local", "sessions");
    Directory.CreateDirectory(folder);
    var path = Path.Combine(folder, $"mafclaw-sample60-{Guid.NewGuid():N}.json");

    // B. Store both sides of an exchange; the deterministic reply is explicitly not model inference.
    async Task RunTurnAsync(string text)
    {
        transcript.Add(new Turn("user", text));
        var reply = $"This is transcript message #{transcript.Count + 1}.";
        transcript.Add(new Turn("assistant", reply));
        Console.WriteLine($"> {text}\n{reply}");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(transcript));
    }

    try
    {
        await RunTurnAsync("My name is Ada.");
        await RunTurnAsync("I prefer email over calls.");
        var savedCount = transcript.Count;
        Console.WriteLine($"SAVED {savedCount} messages to {path}.");

        // C. Simulate a restart with a fresh list from disk. Open the saved file to inspect all six messages.
        transcript = JsonSerializer.Deserialize<List<Turn>>(await File.ReadAllTextAsync(path))
            ?? throw new InvalidOperationException("The saved transcript was empty.");
        Console.WriteLine($"RESTORED {transcript.Count} messages.");
        await RunTurnAsync("Continue after the restart.");
        var passed = savedCount == 4 && transcript.Count == 6;
        Console.WriteLine(passed ? "SESSION PERSISTENCE PRIMITIVE PASS" : "SESSION PERSISTENCE PRIMITIVE FAIL");
        return passed ? 0 : 1;
    }
    finally
    {
        if (selfTest) File.Delete(path);
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Session persistence sample failed: {exception.GetType().Name}");
    return 1;
}
