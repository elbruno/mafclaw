// Objective: run one real on-device chat completion with Foundry Local, no cloud endpoint at all.
// A. Start (or attach to) the local Foundry Local runtime and open its model catalog.
// B. Ensure a small chat model is downloaded and loaded, then start its OpenAI-compatible web service.
// C. Call it with the real, official OpenAI SDK against http://127.0.0.1 and print the on-device answer.
using System.ClientModel;
using System.Net;
using System.Net.Sockets;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;

const string Alias = "phi-4-mini"; // small, tool-capable chat model; ~3.6 GB once cached
const string Question = "In one sentence, what is Retrieval-Augmented Generation?";

if (args.Length == 1 && args[0] == "--describe")
{
    Console.WriteLine($"FOUNDRY LOCAL DESCRIBE: would start the Foundry Local runtime, resolve model \"{Alias}\" " +
        "from its catalog, download/load it if needed, start its local OpenAI-compatible web service, then call " +
        $"it with the official OpenAI SDK to ask \"{Question}\". Everything runs on this machine; no cloud endpoint is used.");
    Console.WriteLine("No model was downloaded, loaded, or run.");
    return 2;
}

try
{
    if (args.Length != 1 || args[0] != "--live")
        throw new InvalidOperationException("Usage: --describe | --live");

    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10));

    // FoundryLocalManager.CreateAsync starts (or attaches to) the native Foundry Local runtime in-process.
    var configuration = new Configuration
    {
        AppName = "MafClaw.Sample70",
        Web = new Configuration.WebService { Urls = $"http://127.0.0.1:{FindFreePort()}" }
    };
    await FoundryLocalManager.CreateAsync(configuration, NullLogger.Instance, deadline.Token);
    var manager = FoundryLocalManager.Instance;

    var catalog = await manager.GetCatalogAsync(deadline.Token);
    var model = await catalog.GetModelAsync(Alias, deadline.Token)
        ?? throw new InvalidOperationException($"Model \"{Alias}\" was not found in the Foundry Local catalog.");
    if (!await model.IsCachedAsync(deadline.Token))
    {
        Console.WriteLine($"Downloading {Alias} (first run only)...");
        await model.DownloadAsync(progress => Console.Write($"\r  {progress:F0}%   "), deadline.Token);
        Console.WriteLine();
    }
    if (!await model.IsLoadedAsync(deadline.Token))
    {
        Console.WriteLine($"Loading {Alias} into the local runtime...");
        await model.LoadAsync(deadline.Token);
    }

    await manager.StartWebServiceAsync(deadline.Token);
    var baseUrl = manager.Urls?.FirstOrDefault()
        ?? throw new InvalidOperationException("Foundry Local did not report a bound web service URL.");
    Console.WriteLine($"Foundry Local web service ready at {baseUrl}");

    // The official OpenAI SDK talks to the local, OpenAI-compatible endpoint; no API key is actually checked.
    var client = new OpenAIClient(new ApiKeyCredential("not-needed"),
        new OpenAIClientOptions { Endpoint = new Uri($"{baseUrl}/v1") });
    var chatClient = client.GetChatClient(model.Id);

    Console.WriteLine($"Asking {Alias}: {Question}");
    var completion = (await chatClient.CompleteChatAsync(
        [new OpenAI.Chat.UserChatMessage(Question)], cancellationToken: deadline.Token)).Value;
    var answer = string.Concat(completion.Content.Select(part => part.Text));
    Console.WriteLine($"ON-DEVICE ANSWER: {answer}");

    // Best-effort teardown: a benign "session still in use" race while HTTP keep-alive winds down must not
    // override a completion that already succeeded, so cleanup failures are logged, not thrown.
    try { await model.UnloadAsync(deadline.Token); }
    catch (Exception cleanupException) { Console.Error.WriteLine($"(non-fatal) model unload: {cleanupException.GetType().Name}"); }
    try { await manager.StopWebServiceAsync(deadline.Token); manager.Shutdown(); }
    catch (Exception cleanupException) { Console.Error.WriteLine($"(non-fatal) web service shutdown: {cleanupException.GetType().Name}"); }

    var passed = !string.IsNullOrWhiteSpace(answer);
    Console.WriteLine(passed ? "FOUNDRY LOCAL CLIENT PASS" : "FOUNDRY LOCAL CLIENT FAIL");
    return passed ? 0 : 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Foundry Local client failed: {exception.GetType().Name}: {exception.Message}");
    return 1;
}

static int FindFreePort()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}
