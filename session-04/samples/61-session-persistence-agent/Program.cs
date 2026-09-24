// Objective: Sample 61 (MAF/Harness) saves and restores a real AgentSession in one dotnet run.
// A. Build the agent here; Chat Completions leaves conversation history with the Harness.
// B. Tell the agent two facts and serialize its session to a local JSON file.
// C. Restore a fresh session and prove the earlier messages are sent to the model again.

using System.Text.Json;
using MafClaw.Samples;
using MafClaw.Session04.Samples;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

try
{
    var fixture = args.Contains("--fixture");
    var selfTest = args.Contains("--self-test");
    if (args.Any(argument => argument is not ("--fixture" or "--self-test" or "--live")) ||
        (fixture && args.Contains("--live")) || (selfTest && !fixture))
        throw new InvalidOperationException("Run with dotnet run. Automated check: --fixture --self-test.");
    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));

    // A. RecordingChatClient observes outbound history; it does not create or restore memory.
    // A fixed fixture answer cannot prove recall, so step C inspects the actual resent messages.
    using var recorder = new RecordingChatClient(fixture
        ? new FixtureChatClient(
            FixtureChatClient.Text("Nice to meet you, Ada."),
            FixtureChatClient.Text("Noted: you prefer email over calls."),
            FixtureChatClient.Text("Your name is Ada and you prefer email over calls."))
        : DemoSettings.Load().CreateChatClient());
    var agent = recorder.AsHarnessAgent(new HarnessAgentOptions
    {
        Name = "SessionPersistenceDemo",
        DisableFileMemory = true, DisableAgentSkillsProvider = true, DisableWebSearch = true,
        DisableOpenTelemetry = true, DisableToolAutoApproval = true,
        DisableTodoProvider = true, DisableAgentModeProvider = true,
        ChatOptions = new ChatOptions { Instructions = "Remember what the user tells you in this conversation. Answer briefly." }
    });

    // B. SerializeSessionAsync owns the MAF session schema; we only choose the destination.
    Console.WriteLine(fixture ? "FIXTURE inference; REAL MAF session persistence." : "LIVE model; local MAF session persistence.");
    var session = await agent.CreateSessionAsync(deadline.Token);
    DemoOutput.Print(await agent.RunAsync("My name is Ada.", session, cancellationToken: deadline.Token));
    DemoOutput.Print(await agent.RunAsync("I prefer email over calls.", session, cancellationToken: deadline.Token));
    var folder = selfTest ? Path.GetTempPath() : Path.Combine(Environment.CurrentDirectory, ".local", "sessions");
    Directory.CreateDirectory(folder);
    var path = Path.Combine(folder, $"mafclaw-sample61-{Guid.NewGuid():N}.json");
    var serialized = await agent.SerializeSessionAsync(session, cancellationToken: deadline.Token);
    await File.WriteAllTextAsync(path, serialized.GetRawText(), deadline.Token);
    Console.WriteLine($"SAVED session to {path}.");

    // C. A new session object comes from disk, not the old variable. Open the JSON to show its history.
    try
    {
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path, deadline.Token));
        var restored = await agent.DeserializeSessionAsync(document.RootElement, cancellationToken: deadline.Token);
        Console.WriteLine("RESTORED a fresh AgentSession from JSON.");
        DemoOutput.Print(await agent.RunAsync("What's my name and my contact preference?", restored,
            cancellationToken: deadline.Token));
        var passed = recorder.LastMessages.Any(message => message.Text.Contains("My name is Ada", StringComparison.OrdinalIgnoreCase))
            && recorder.LastMessages.Any(message => message.Text.Contains("prefer email", StringComparison.OrdinalIgnoreCase));
        Console.WriteLine(passed
            ? "SESSION PERSISTENCE AGENT PASS: restored session actually resent prior turns."
            : "SESSION PERSISTENCE AGENT FAIL: restored session lost prior turns.");
        return passed ? 0 : 1;
    }
    finally
    {
        if (selfTest) File.Delete(path);
    }
}
catch (Exception exception) { return DemoOutput.Report(exception); }
