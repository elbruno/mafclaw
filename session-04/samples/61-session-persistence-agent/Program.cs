// Objective: persist and restore an actual MAF AgentSession, not a hand-rolled message list.
// A. Build a minimal harness agent with scripted or live inference.
// B. Run two turns, then serialize the session to JSON.
// C. Deserialize it into a fresh call and prove prior turns really reached the model.
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using MafClaw.Session04;
using MafClaw.Session04.Samples;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

try
{
    var fixture = args.Contains("--fixture");
    var live = args.Contains("--live");
    var selfTest = args.Contains("--self-test");
    if (fixture == live) throw new FinanceConfigurationException("Usage: --fixture [--self-test] | --live [--save <path>] [--resume <path>]");
    if (selfTest && !fixture) throw new FinanceConfigurationException("--self-test requires --fixture; it never silently calls a model.");

    // The fixture never reads session content: every scripted reply is a fixed stand-in. Real recall
    // is proven separately below, by inspecting the exact messages the recording client received.
    var recorder = new RecordingChatClient(fixture
        ? new ScriptedChatClient(
            ScriptedChatClient.Text("Nice to meet you, Ada."),
            ScriptedChatClient.Text("Noted: you prefer email over calls."),
            ScriptedChatClient.Text("Your name is Ada and you prefer email over calls."))
        : new AIProjectClient(FinanceSettings.Load().ProjectEndpoint, new AzureCliCredential())
            .GetProjectOpenAIClient().GetResponsesClient().AsIChatClient(FinanceSettings.Load().Model));

    var agent = recorder.AsHarnessAgent(new HarnessAgentOptions
    {
        Name = "SessionPersistenceDemo",
        DisableFileMemory = true, DisableAgentSkillsProvider = true, DisableWebSearch = true,
        DisableOpenTelemetry = true, DisableToolAutoApproval = true,
        DisableTodoProvider = true, DisableAgentModeProvider = true,
        ChatOptions = new ChatOptions { Instructions = "You are a small demo assistant. Remember what the user tells you in this session." }
    });

    async Task<string> SaveAsync(string path)
    {
        var session = await agent.CreateSessionAsync();
        FinanceConsole.PrintResponse(await agent.RunAsync("My name is Ada.", session));
        FinanceConsole.PrintResponse(await agent.RunAsync("I prefer email over calls.", session));
        var serialized = await agent.SerializeSessionAsync(session);
        await File.WriteAllTextAsync(path, serialized.GetRawText());
        Console.WriteLine($"SAVED session to {path}.");
        return path;
    }

    async Task<bool> ResumeAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        using var document = JsonDocument.Parse(json);
        var session = await agent.DeserializeSessionAsync(document.RootElement);
        Console.WriteLine($"RESTORED session from {path}.");
        var response = await agent.RunAsync("What's my name and my contact preference?", session);
        FinanceConsole.PrintResponse(response);
        // The scripted reply text is fixed; the real proof is that the restored history was actually sent.
        var sentPriorTurns = recorder.LastMessages.Any(message => message.Text.Contains("My name is Ada", StringComparison.OrdinalIgnoreCase))
            && recorder.LastMessages.Any(message => message.Text.Contains("prefer email", StringComparison.OrdinalIgnoreCase));
        return sentPriorTurns;
    }

    if (selfTest)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mafclaw-sample61-{Guid.NewGuid():N}.json");
        await SaveAsync(path);
        var passed = await ResumeAsync(path);
        File.Delete(path);
        Console.WriteLine(passed
            ? "SESSION PERSISTENCE AGENT PASS: restored session actually resent prior turns."
            : "SESSION PERSISTENCE AGENT FAIL: restored session lost prior turns.");
        return passed ? 0 : 1;
    }

    string? savePath = null, resumePath = null;
    for (var index = 0; index < args.Length; index++)
    {
        if (args[index] == "--save" && index + 1 < args.Length) savePath = args[++index];
        if (args[index] == "--resume" && index + 1 < args.Length) resumePath = args[++index];
    }
    if (resumePath is not null) { await ResumeAsync(resumePath); return 0; }
    await SaveAsync(savePath ?? Path.Combine(Path.GetTempPath(), $"mafclaw-sample61-{Guid.NewGuid():N}.json"));
    return 0;
}
catch (Exception exception) { return SafeErrors.Report(exception); }
