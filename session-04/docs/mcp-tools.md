# MCP tools

Samples 50/51 teach [Model Context Protocol](https://modelcontextprotocol.io)
(MCP) tool consumption: first the raw client protocol, then two MAF agents
that use tools sourced two different ways. Both samples call the public
[Microsoft Learn MCP server](https://learn.microsoft.com/api/mcp)
(`https://learn.microsoft.com/api/mcp`, Streamable HTTP, no authentication).
Your use of any third-party MCP server is subject to that provider's terms;
review the [MCP security best practices](https://modelcontextprotocol.io/specification/draft/basic/security_best_practices)
before adding an untrusted server.

## 50: the raw protocol, no agent

`MafClaw.Sample50` uses the official
[MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
(`ModelContextProtocol` NuGet package) directly:

1. `McpClient.CreateAsync` performs the real MCP `initialize` handshake over
   `HttpClientTransport` (`HttpTransportMode.StreamableHttp`).
2. `ListToolsAsync` discovers the server's tools (`microsoft_docs_search`,
   `microsoft_docs_fetch`, `microsoft_code_sample_search`).
3. `CallToolAsync` calls `microsoft_docs_search` directly and prints the raw
   structured result — no model is involved. This is exactly the call an
   agent's function-invocation loop makes once a model decides to use the tool.

```powershell
dotnet run --project .\samples\50-mcp-tools\MafClaw.Sample50.csproj -- --describe
dotnet run --project .\samples\50-mcp-tools\MafClaw.Sample50.csproj -- --live
```

`--describe` prints the planned connection/tool call and makes no network
request (offline, exit 2). `--live` makes a real request to the public server
(exit 0 on success); it requires outbound internet access but no credentials.

## 51: two MAF agents, two tool sources

`MafClaw.Sample51` builds two agents that answer the same fixed technical
question ("What's new in .NET 10?") so the audience can compare grounding
sources side by side:

| Agent | Tool source | MAF integration point |
|---|---|---|
| `WebSearchAgent` | The framework's built-in `HostedWebSearchTool` (`Microsoft.Extensions.AI`) | `IChatClient.AsAIAgent(tools: [new HostedWebSearchTool()])`. The host model provider executes the search server-side; this saves us from implementing any search integration ourselves. |
| `MicrosoftLearnAgent` | The real Microsoft Learn MCP server | `McpClientTool` results are cast to `AITool` and passed to `HarnessAgentOptions.ChatOptions.Tools`. The harness's own function-invocation loop calls the MCP tool as a real `AIFunction`, saving us from hand-rolling tool-call dispatch and MCP protocol plumbing. |

```powershell
dotnet run --project .\samples\51-mcp-tools-agent\MafClaw.Sample51.csproj -- --fixture
dotnet run --project .\samples\51-mcp-tools-agent\MafClaw.Sample51.csproj -- --live
```

`HostedWebSearchTool` executes entirely server-side during a live model call,
so a fixture cannot invoke it for real: `--fixture` shows a clearly labeled
scripted stand-in for `WebSearchAgent`. `MicrosoftLearnAgent`'s MCP tool is a
local `AIFunction`, so even `--fixture` performs a **real** MCP tool call
against the live Microsoft Learn server — only the model turn is scripted.
`--live` uses the configured Foundry model for both agents and can incur
charges.

## What "MCP tools with an agent harness" means here

MCP standardizes *how* an agent discovers and calls external tools. The
harness (or `ChatClientAgent`) still owns *whether and when* to call them —
tool selection remains a model decision, approval/authority policy remains
the host's, and MCP tools are ordinary `AITool`/`AIFunction` instances once
listed. Nothing about MCP bypasses the approval/governance patterns shown in
[governance](governance.md).
