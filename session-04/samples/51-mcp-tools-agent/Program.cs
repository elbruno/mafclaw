// Objective: contrast a hosted built-in tool with a real external MCP tool inside MAF agents.
// A. Build a web-search agent using the framework's HostedWebSearchTool.
// B. Build a Microsoft Learn agent wired to real MCP tools over Streamable HTTP.
// C. Ask both agents the same technical question and show their tool evidence.
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using MafClaw.Session04;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

const string Question = "What's new in .NET 10?";
const string LearnEndpoint = "https://learn.microsoft.com/api/mcp";

try
{
    if (args.Length != 1 || args[0] is not ("--fixture" or "--live"))
        throw new FinanceConfigurationException("Usage: --fixture | --live");
    var fixture = args[0] == "--fixture";
    var liveClient = fixture ? null : new AIProjectClient(FinanceSettings.Load().ProjectEndpoint, new AzureCliCredential())
        .GetProjectOpenAIClient().GetResponsesClient().AsIChatClient(FinanceSettings.Load().Model);

    // Agent 1: the framework's own hosted web search tool. It executes server-side during a live model
    // call, so a fixture cannot invoke it for real; the fixture shows a clearly labeled scripted stand-in.
    IChatClient webSearchClient = fixture
        ? new ScriptedChatClient(ScriptedChatClient.Text(
            "[FIXTURE web search] .NET 10 highlights: performance, C# language updates, and library " +
            "improvements. (Scripted stand-in: hosted web search requires a live model.)"))
        : liveClient!;
    var webSearchAgent = webSearchClient.AsAIAgent(name: "WebSearchAgent",
        description: "Answers using the framework's hosted web search tool.",
        instructions: "Answer the question using web search. Cite sources when possible.",
        tools: [new HostedWebSearchTool()]);

    Console.WriteLine($"=== WebSearchAgent ({(fixture ? "FIXTURE" : "LIVE")}) ===");
    var webSearchResponse = await webSearchAgent.RunAsync(Question, await webSearchAgent.CreateSessionAsync());
    FinanceConsole.PrintResponse(webSearchResponse);

    // Agent 2: a real MCP server. McpClient performs the actual MCP handshake over HTTP.
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    await using var mcpClient = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri(LearnEndpoint), Name = "Microsoft Learn MCP", TransportMode = HttpTransportMode.StreamableHttp
    }), cancellationToken: deadline.Token);
    var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: deadline.Token);
    Console.WriteLine($"Microsoft Learn MCP offers {mcpTools.Count} tool(s): {string.Join(", ", mcpTools.Select(tool => tool.Name))}");

    // The harness's own function-invocation loop calls MCP tools as real AIFunctions, so the fixture
    // below still performs the actual MCP round trip; only the model turn is scripted.
    IChatClient learnClient = fixture
        ? new ScriptedChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new FunctionCallContent("fixture-learn", "microsoft_docs_search",
                    new Dictionary<string, object?> { ["query"] = Question })])),
            ScriptedChatClient.Text("[FIXTURE final answer over the REAL microsoft_docs_search result above.]"))
        : liveClient!;
    var learnAgent = learnClient.AsHarnessAgent(new HarnessAgentOptions
    {
        Name = "MicrosoftLearnAgent",
        DisableFileMemory = true, DisableAgentSkillsProvider = true, DisableWebSearch = true,
        DisableOpenTelemetry = true, DisableToolAutoApproval = true,
        DisableTodoProvider = true, DisableAgentModeProvider = true,
        ChatOptions = new ChatOptions
        {
            Tools = [.. mcpTools.Cast<AITool>()],
            Instructions = "Answer using the microsoft_docs_search / microsoft_docs_fetch tools. Cite the source URL."
        }
    });

    Console.WriteLine($"=== MicrosoftLearnAgent ({(fixture ? "FIXTURE inference, REAL MCP tool call" : "LIVE")}) ===");
    var learnResponse = await learnAgent.RunAsync(Question, await learnAgent.CreateSessionAsync(deadline.Token),
        cancellationToken: deadline.Token);
    FinanceConsole.PrintResponse(learnResponse);

    var learnToolEvidence = learnResponse.Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Any();
    var passed = !string.IsNullOrWhiteSpace(webSearchResponse.Text) && learnToolEvidence;
    Console.WriteLine(passed ? "MCP AGENTS PASS" : "MCP AGENTS FAIL: missing evidence");
    return passed ? 0 : 1;
}
catch (Exception exception) { return SafeErrors.Report(exception); }
