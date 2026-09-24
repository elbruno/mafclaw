// Objective: show what "session state" means before any framework persists it for you.
// A. Keep an explicit, serializable list of turns instead of relying on hidden model memory.
// B. Save the list to a JSON file after each turn.
// C. Reload it with --resume and keep numbering the same conversation.
using System.Text.Json;
using MafClaw.Session04.Samples;

try
{
    string? resumePath = null;
    for (var index = 0; index < args.Length; index++)
        if (args[index] == "--resume" && index + 1 < args.Length) resumePath = args[++index];
    var selfTest = args.Contains("--self-test");

    List<Turn> transcript = [];
    var path = resumePath ?? Path.Combine(Path.GetTempPath(), $"mafclaw-sample60-{Guid.NewGuid():N}.json");
    if (resumePath is not null && File.Exists(resumePath))
    {
        transcript = JsonSerializer.Deserialize<List<Turn>>(await File.ReadAllTextAsync(resumePath)) ?? [];
        Console.WriteLine($"RESUMED {transcript.Count} prior message(s) from {resumePath}.");
    }
    else
    {
        Console.WriteLine(resumePath is null
            ? $"Starting a NEW transcript; will save to {path}."
            : $"No existing transcript at {resumePath}; starting new.");
    }

    async Task RunTurnAsync(string userText)
    {
        transcript.Add(new Turn("user", userText));
        // No model is involved; this deterministic reply stands in for "the assistant".
        var reply = $"Got it: this is message #{transcript.Count + 1}. You said: \"{userText}\"";
        transcript.Add(new Turn("assistant", reply));
        Console.WriteLine($"> {userText}");
        Console.WriteLine(reply);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(transcript));
    }

    if (selfTest)
    {
        await RunTurnAsync("My name is Ada.");
        await RunTurnAsync("What is 2 + 2?");
        var savedCount = transcript.Count;
        // Simulate a process restart: reload the transcript from disk into a fresh list.
        transcript = JsonSerializer.Deserialize<List<Turn>>(await File.ReadAllTextAsync(path)) ?? [];
        await RunTurnAsync("Continue after the restart.");
        var passed = savedCount == 4 && transcript.Count == 6;
        Console.WriteLine(passed ? "SESSION PERSISTENCE PRIMITIVE PASS" : "SESSION PERSISTENCE PRIMITIVE FAIL");
        File.Delete(path);
        return passed ? 0 : 1;
    }

    Console.WriteLine("Commands: /exit. This is a local transcript demo; not financial advice.");
    while (true)
    {
        Console.Write("> ");
        var input = await Console.In.ReadLineAsync();
        if (input is null || input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.IsNullOrWhiteSpace(input)) continue;
        await RunTurnAsync(input);
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Session persistence sample failed: {exception.GetType().Name}");
    return 1;
}
