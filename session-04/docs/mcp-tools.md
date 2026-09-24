# MCP tools

Samples 50/51 teach [Model Context Protocol](https://modelcontextprotocol.io):
first the raw client, then one MAF/Harness agent using the same tools.
Both use the public [Microsoft Learn MCP server](https://learn.microsoft.com/api/mcp)
over Streamable HTTP, without MCP credentials.

From either sample's directory, run:

```powershell
dotnet run
```

## 50: raw protocol, no model

`McpClient.CreateAsync` performs the handshake, `ListToolsAsync` discovers the
tools, and `CallToolAsync` calls `microsoft_docs_search`. The program prints the
real result. It needs outbound internet access, but no model or Azure login.

## 51: one agent, real MCP tools

`Program.cs` visibly passes the discovered `McpClientTool` objects as `AITool`
instances to `HarnessAgentOptions.ChatOptions.Tools`. The Harness selects,
dispatches and feeds back the tool result. It answers a fixed question about
.NET 10 and checks for a nonempty answer plus successful tool-result evidence.

Configure the Foundry endpoint/model once using the repository setup script.
The sample uses Chat Completions and explicitly selects
`ReasoningEffort.None` for this small tool demo: GPT-6 Chat Completions rejects
function tools with reasoning enabled.

The earlier hosted web-search comparison was removed. It introduced an
unrelated `web_search_preview` capability requirement that the configured
model rejected. This is not a fallback to pretend search: the displayed
Microsoft Learn MCP result is a real network tool result.

## Offline versus live

Sample 50 `--describe` returns 2 without a network request. Sample 51
`--fixture` replaces inference with scripted responses, but still performs a
real MCP call; the verification manifest deliberately classifies it as network
work, not offline. Normal `dotnet run` uses real model inference and can incur
charges. Request failures return nonzero and include the HTTP status without
dumping private provider payloads.

MCP standardizes discovery and calling, not permission. Tool selection remains
the model's decision and approval policy remains the host's. Review a server's
terms and [security practices](https://modelcontextprotocol.io/specification/draft/basic/security_best_practices)
before adding tools from a new source.
