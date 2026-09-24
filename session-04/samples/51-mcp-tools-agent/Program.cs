// Objective: Sample 51 (MAF/Harness) lets an agent use real Microsoft Learn MCP tools.
// A. Connect to the public MCP server and discover its tools, just like Sample 50.
// B. Pass those tools directly to a Harness built here with the configured model.
// C. Ask one question and show the actual tool result before the grounded answer.

using MafClaw.Samples;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

const string Question = "What's new in .NET 10? Search Microsoft Learn and cite the source.";

try
{
    if (args.Length > 1 || (args.Length == 1 && args[0] is not ("--fixture" or "--live")))
        throw new InvalidOperationException("Run with dotnet run. Automated check: --fixture.");
    var fixture = args.Contains("--fixture");
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));

    // A. ModelContextProtocol.Client owns handshake, discovery and transport; no model is needed yet.
    await using var mcp = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
        Name = "Microsoft Learn MCP",
        TransportMode = HttpTransportMode.StreamableHttp
    }), cancellationToken: deadline.Token);
    var tools = await mcp.ListToolsAsync(cancellationToken: deadline.Token);
    Console.WriteLine($"Microsoft Learn MCP offers {tools.Count} tool(s): {string.Join(", ", tools.Select(tool => tool.Name))}");

    // B. MCP tools already implement AITool. MAF/Harness dispatches calls and feeds results back.
    // The test fixture replaces inference only; it still calls the real public MCP server.
    using IChatClient model = fixture
        ? new FixtureChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new FunctionCallContent("fixture-learn", "microsoft_docs_search",
                    new Dictionary<string, object?> { ["query"] = Question })])),
            FixtureChatClient.Text("[FIXTURE answer over the REAL microsoft_docs_search result above.]"))
        : DemoSettings.Load().CreateChatClient();
    var agent = model.AsHarnessAgent(new HarnessAgentOptions
    {
        Name = "MicrosoftLearnAgent",
        DisableFileMemory = true, DisableAgentSkillsProvider = true, DisableWebSearch = true,
        DisableOpenTelemetry = true, DisableToolAutoApproval = true,
        DisableTodoProvider = true, DisableAgentModeProvider = true,
        ChatOptions = new ChatOptions
        {
            Tools = [.. tools.Cast<AITool>()],

            // Keep this tool lesson short; this deployment requires reasoning off for Chat Completions tools.
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None },
            Instructions = "Use microsoft_docs_search to answer the user's question. Summarize briefly and cite Microsoft Learn URLs."
        }
    });

    // C. One RunAsync drives the model/tool/model loop. Prose alone is not tool evidence.
    Console.WriteLine(fixture ? "FIXTURE inference, REAL MCP tools." : "LIVE model, REAL MCP tools.");
    var response = await agent.RunAsync(Question, await agent.CreateSessionAsync(deadline.Token),
        cancellationToken: deadline.Token);
    DemoOutput.Print(response);
    var hasToolResult = response.Messages.SelectMany(message => message.Contents)
        .OfType<FunctionResultContent>().Any(result => result.Exception is null);
    var passed = !string.IsNullOrWhiteSpace(response.Text) && hasToolResult;
    Console.WriteLine(passed ? "MCP AGENTS PASS" : "MCP AGENTS FAIL: missing answer or successful tool evidence.");
    return passed ? 0 : 1;
}
catch (Exception exception) { return DemoOutput.Report(exception); }
