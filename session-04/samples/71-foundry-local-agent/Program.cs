// Objective: Sample 71 (MAF/Harness) runs a local model and a real tool with dotnet run.
// A. Use ElBruno's Foundry Local adapter to supply IChatClient without an HTTP server.
// B. Build the Harness here with one ordinary C# clock tool.
// C. Ask for the time and verify a real tool result, then release the local model.

using System.ComponentModel;
using ElBruno.MAF.FoundryLocal;
using MafClaw.Samples;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

try
{
    if (args.Length > 1 || (args.Length == 1 && args[0] is not ("--fixture" or "--live")))
        throw new InvalidOperationException("Run with dotnet run. Automated check: --fixture.");
    var fixture = args.Contains("--fixture");
    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10));
    using var logs = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));

    // A. The community adapter owns model download/load and maps local chat to standard IChatClient.
    // It is not a first-party MAF provider. The fixture never initializes the native runtime.
    await using var localModel = new FoundryLocalModelLifecycleService(
        Options.Create(new FoundryLocalOptions { ModelAlias = "qwen2.5-1.5b", DownloadIfMissing = true, UnloadOnExit = true }),
        Options.Create(new ChatRuntimeOptions { MaxOutputTokens = 256, Streaming = false }),
        logs.CreateLogger<FoundryLocalModelLifecycleService>());
    using IChatClient model = fixture
        ? new FixtureChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new FunctionCallContent("fixture-time", "get_local_time", new Dictionary<string, object?>())])),
            FixtureChatClient.Text("[FIXTURE answer over the REAL get_local_time result.]"))
        : new FoundryLocalChatClientAdapter(localModel, logs.CreateLogger<FoundryLocalChatClientAdapter>());

    // B. Microsoft.Extensions.AI creates the tool schema; MAF/Harness owns dispatch and result feedback.
    var toolCalls = 0;
    string? actualTime = null;
    [Description("Get the current local time. You must call this tool to know the time.")]
    string GetLocalTime()
    {
        toolCalls++;
        actualTime = DateTimeOffset.Now.ToString("HH:mm");
        return actualTime;
    }
    var agent = model.AsHarnessAgent(new HarnessAgentOptions
    {
        Name = "FoundryLocalAgent",
        DisableFileMemory = true, DisableAgentSkillsProvider = true, DisableWebSearch = true,
        DisableOpenTelemetry = true, DisableToolAutoApproval = true,
        DisableTodoProvider = true, DisableAgentModeProvider = true,
        ChatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(GetLocalTime, "get_local_time")],
            MaxOutputTokens = 256,
            Temperature = 0,
            Instructions = "Call get_local_time once. Then repeat the exact time returned by the tool, and nothing else. Never invent or convert the time."
        }
    });

    // C. The larger 1.5B model is used for tool fidelity; first use downloads about 1.3 GB.
    Console.WriteLine(fixture ? "FIXTURE inference; REAL local tool." : "LOCAL qwen2.5-1.5b; no cloud credentials or HTTP server.");
    var response = await agent.RunAsync("What is the current time? Call get_local_time to find out.",
        await agent.CreateSessionAsync(deadline.Token), cancellationToken: deadline.Token);
    DemoOutput.Print(response);
    var passed = toolCalls > 0 && actualTime is not null && (fixture || response.Text.Contains(actualTime, StringComparison.Ordinal)) &&
        response.Messages.SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>().Any(result => result.Exception is null);
    Console.WriteLine(passed ? "FOUNDRY LOCAL AGENT PASS" : "FOUNDRY LOCAL AGENT FAIL: missing tool call or answer does not match the actual time.");
    return passed ? 0 : 1;
}
catch (Exception exception) { return DemoOutput.Report(exception); }
