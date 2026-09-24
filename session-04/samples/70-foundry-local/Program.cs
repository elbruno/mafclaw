// Objective: Sample 70 (plain C#, no MAF) calls a local model with dotnet run.
// A. Start Foundry Local in-process and select a small model.
// B. Download it only if missing, then load it.
// C. Ask one question through the native SDK and unload the model.

using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging.Abstractions;

if (args.SequenceEqual(["--describe"]))
{
    Console.WriteLine("FOUNDRY LOCAL DESCRIBE: qwen2.5-0.5b, in-process SDK, no cloud credentials or HTTP server.");
    Console.WriteLine("No model was downloaded, loaded, or run.");
    return 2;
}

try
{
    if (args.Length > 1 || (args.Length == 1 && args[0] != "--live"))
        throw new InvalidOperationException("Run with dotnet run. Offline preview: --describe.");
    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10));

    // A. Microsoft.AI.Foundry.Local owns the native runtime; no REST server or port is needed.
    await FoundryLocalManager.CreateAsync(new Configuration { AppName = "ElBruno_MAF_FoundryLocal" },
        NullLogger.Instance, deadline.Token);
    var manager = FoundryLocalManager.Instance;
    var catalog = await manager.GetCatalogAsync(deadline.Token);
    var model = await catalog.GetModelAsync("qwen2.5-0.5b", deadline.Token)
        ?? throw new InvalidOperationException("qwen2.5-0.5b was not found in the local model catalog.");

    // B. Cache this model once; Sample 71 downloads its own larger, tool-capable model.
    if (!await model.IsCachedAsync(deadline.Token))
    {
        Console.WriteLine("Downloading qwen2.5-0.5b (about 528 MB, first run only)...");
        await model.DownloadAsync(progress => Console.Write($"\r  {progress:F0}%   "), deadline.Token);
        Console.WriteLine();
    }
    await model.LoadAsync(deadline.Token);
    try
    {
        // C. This is the raw SDK client. Sample 71 adapts the same local engine to MAF's IChatClient.
        using var session = new ChatSession(model);
        session.SetOptions(new RequestOptions { Search = new SearchOptions { MaxOutputTokens = 128 } });
        using var request = new Request();
        request.AddItem(MessageItem.User("Explain local AI in one short sentence."));
        using var response = await session.ProcessRequestAsync(request, deadline.Token);
        var answer = string.Concat(response.OfType<MessageItem>().Select(message => message.GetSimpleText()));
        Console.WriteLine($"ON-DEVICE ANSWER: {answer}");
        var passed = !string.IsNullOrWhiteSpace(answer);
        Console.WriteLine(passed ? "FOUNDRY LOCAL CLIENT PASS" : "FOUNDRY LOCAL CLIENT FAIL: no completed answer.");
        return passed ? 0 : 1;
    }
    finally
    {
        await model.UnloadAsync();
        manager.Shutdown();
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Foundry Local failed: {exception.GetType().Name}. Check the native runtime and model availability.");
    return 1;
}
