// Objective: bridge Foundry Local's on-device chat completion into a real MAF harness agent.
// A. Wire a local_time tool the model can call without any network access.
// B. Fixture proves the agent/tool wiring deterministically; no Foundry Local dependency.
// C. Live starts Foundry Local, bridges its OpenAI-compatible client to IChatClient, and runs for real.
using System.ClientModel;
using System.Net;
using System.Net.Sockets;
using MafClaw.Session04;
using Microsoft.AI.Foundry.Local;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;

const string Alias = "phi-4-mini";
const string Question = "What is the current local time on this machine? Use the tool, then explain in one " +
    "sentence why on-device inference keeps that question's answer off the network entirely.";

[System.ComponentModel.Description("Returns the current local wall-clock time on this on-device host. Runs entirely locally; no network call.")]
static string GetLocalTime() => DateTimeOffset.Now.ToString("f");

try
{
    if (args.Length != 1 || args[0] is not ("--fixture" or "--live"))
        throw new FinanceConfigurationException("Usage: --fixture | --live");
    var fixture = args[0] == "--fixture";

    var tools = new List<AITool> { AIFunctionFactory.Create((Func<string>)GetLocalTime, "get_local_time") };
    Microsoft.AI.Foundry.Local.FoundryLocalManager? manager = null;
    Microsoft.AI.Foundry.Local.IModel? model = null;
    IChatClient chatClient;

    if (fixture)
    {
        // The fixture never talks to Foundry Local; it only proves the harness calls the real local tool
        // and folds its real result back into a scripted final answer.
        chatClient = new ScriptedChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new FunctionCallContent("fixture-time", "get_local_time", new Dictionary<string, object?>())])),
            ScriptedChatClient.Text("[FIXTURE final answer over the REAL get_local_time result above.] " +
                "On-device inference means the model, the tool call, and the answer never leave this machine."));
    }
    else
    {
        using var startupDeadline = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var configuration = new Configuration
        {
            AppName = "MafClaw.Sample71",
            Web = new Configuration.WebService { Urls = $"http://127.0.0.1:{FindFreePort()}" }
        };
        await FoundryLocalManager.CreateAsync(configuration, NullLogger.Instance, startupDeadline.Token);
        manager = FoundryLocalManager.Instance;

        var catalog = await manager.GetCatalogAsync(startupDeadline.Token);
        model = await catalog.GetModelAsync(Alias, startupDeadline.Token)
            ?? throw new InvalidOperationException($"Model \"{Alias}\" was not found in the Foundry Local catalog.");
        if (!await model.IsCachedAsync(startupDeadline.Token))
            await model.DownloadAsync(progress => Console.Write($"\r  downloading {progress:F0}%   "), startupDeadline.Token);
        if (!await model.IsLoadedAsync(startupDeadline.Token))
            await model.LoadAsync(startupDeadline.Token);
        await manager.StartWebServiceAsync(startupDeadline.Token);
        var baseUrl = manager.Urls?.FirstOrDefault()
            ?? throw new InvalidOperationException("Foundry Local did not report a bound web service URL.");
        Console.WriteLine($"Foundry Local web service ready at {baseUrl} (on-device, no cloud endpoint)");

        // The real, official OpenAI SDK client, bridged to IChatClient by Microsoft.Extensions.AI.OpenAI.
        var openAiClient = new OpenAIClient(new ApiKeyCredential("not-needed"),
            new OpenAIClientOptions { Endpoint = new Uri($"{baseUrl}/v1") });
        chatClient = openAiClient.GetChatClient(model.Id).AsIChatClient();
    }

    // The result is computed first and only returned after cleanup, so a benign teardown error (for example the
    // native runtime briefly reporting the model session as still in use while HTTP keep-alive winds down) can
    // never silently overwrite a PASS that already happened.
    int exitCode;
    try
    {
        var agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
        {
            Name = "FoundryLocalAgent",
            DisableFileMemory = true, DisableAgentSkillsProvider = true, DisableWebSearch = true,
            DisableOpenTelemetry = true, DisableToolAutoApproval = true,
            DisableTodoProvider = true, DisableAgentModeProvider = true,
            ChatOptions = new ChatOptions
            {
                Tools = tools,
                // Small on-device models often narrate "[calling the tool...]" in plain text instead of emitting a
                // real tool_calls payload when left to decide for themselves (ChatToolMode.Auto). Forcing this
                // specific function makes phi-4-mini actually invoke it through the OpenAI-compatible protocol.
                ToolMode = ChatToolMode.RequireSpecific("get_local_time"),
                Instructions = "Answer using the get_local_time tool when asked about the current time. Be concise."
            }
        });

        Console.WriteLine($"=== FoundryLocalAgent ({(fixture ? "FIXTURE inference, REAL tool call" : $"LIVE on-device {Alias}")}) ===");
        using var runDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var response = await agent.RunAsync(Question, await agent.CreateSessionAsync(runDeadline.Token),
            cancellationToken: runDeadline.Token);
        FinanceConsole.PrintResponse(response);

        var toolEvidence = response.Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Any();
        var passed = !string.IsNullOrWhiteSpace(response.Text) && toolEvidence;
        Console.WriteLine(passed ? "FOUNDRY LOCAL AGENT PASS" : "FOUNDRY LOCAL AGENT FAIL: missing evidence");
        exitCode = passed ? 0 : 1;
    }
    finally
    {
        // Best-effort teardown: log and continue rather than let a cleanup race override the run's real result.
        try
        {
            if (model is not null) await model.UnloadAsync();
        }
        catch (Exception cleanupException)
        {
            Console.Error.WriteLine($"(non-fatal) model unload: {cleanupException.GetType().Name}");
        }
        try
        {
            if (manager is not null) { await manager.StopWebServiceAsync(); manager.Shutdown(); }
        }
        catch (Exception cleanupException)
        {
            Console.Error.WriteLine($"(non-fatal) web service shutdown: {cleanupException.GetType().Name}");
        }
    }
    return exitCode;
}
catch (Exception exception) { return SafeErrors.Report(exception); }

static int FindFreePort()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}
