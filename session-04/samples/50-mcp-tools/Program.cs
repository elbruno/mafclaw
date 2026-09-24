// Objective: show the raw MCP client protocol before any agent wraps it.
// A. Connect to a real public MCP server over Streamable HTTP.
// B. List the tools it exposes.
// C. Call one tool directly and print its raw structured result.
using System.Text.Json;
using ModelContextProtocol.Client;

const string Endpoint = "https://learn.microsoft.com/api/mcp";
const string Query = "What's new in .NET 10?";

if (args.Length == 1 && args[0] == "--describe")
{
    Console.WriteLine($"MCP DESCRIBE: would connect to {Endpoint} (Streamable HTTP, no auth), list its tools, " +
        $"then call microsoft_docs_search with query \"{Query}\".");
    Console.WriteLine("No network request was made.");
    return 2;
}

try
{
    if (args.Length != 1 || args[0] != "--live")
        throw new InvalidOperationException("Usage: --describe | --live");

    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    var transport = new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri(Endpoint),
        Name = "Microsoft Learn MCP",
        TransportMode = HttpTransportMode.StreamableHttp
    });

    // McpClient.CreateAsync performs the real MCP initialize handshake over HTTP.
    await using var client = await McpClient.CreateAsync(transport, cancellationToken: deadline.Token);

    var tools = await client.ListToolsAsync(cancellationToken: deadline.Token);
    Console.WriteLine($"Discovered {tools.Count} tool(s) on {Endpoint}:");
    foreach (var tool in tools) Console.WriteLine($"  - {tool.Name}: {tool.Description}");

    var searchTool = tools.FirstOrDefault(tool => tool.Name == "microsoft_docs_search")
        ?? throw new InvalidOperationException("microsoft_docs_search was not offered by this server.");

    // A direct CallToolAsync bypasses any model; this is the same call an agent's function-invocation loop makes.
    var result = await client.CallToolAsync(searchTool.Name,
        new Dictionary<string, object?> { ["query"] = Query }, cancellationToken: deadline.Token);

    Console.WriteLine($"RAW TOOL RESULT for \"{Query}\":");
    foreach (var content in result.Content) Console.WriteLine(JsonSerializer.Serialize(content));

    var passed = result.Content.Count > 0 && result.IsError != true;
    Console.WriteLine(passed ? "MCP CLIENT PASS" : "MCP CLIENT FAIL");
    return passed ? 0 : 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"MCP client failed: {exception.GetType().Name}: {exception.Message}");
    return 1;
}
