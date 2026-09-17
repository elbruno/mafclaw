// Objective: keep returned hosted-search evidence separate from model narrative.
// A. Observe actual MAF WebSearchToolCallContent and WebSearchToolResultContent.
// B. Preserve returned source URLs when the background provider extracts plain text.
// C. Explicitly report missing search evidence without fabricating tool events.

using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample42;

public sealed class HostedNewsTracingClient(IChatClient inner, Transcript transcript) : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);
        var sources = new HashSet<string>(StringComparer.Ordinal);
        bool observedSearch = false;
        foreach (AIContent content in response.Messages.SelectMany(message => message.Contents))
        {
            // A. Hosted search runs inside the service, not FunctionInvokingChatClient.
            if (content is WebSearchToolCallContent call)
            {
                observedSearch = true;
                transcript.Write("NewsAgent", "TOOL_CALL", $"hosted_web_search [{call.CallId}] (returned service event)");
            }
            if (content is WebSearchToolResultContent result)
            {
                string[] urls = result.Outputs?.OfType<UriContent>()
                    .Select(item => item.Uri.ToString()).ToArray() ?? [];
                sources.UnionWith(urls);
                transcript.Write("NewsAgent", "TOOL_RESULT",
                    $"hosted_web_search [{result.CallId}] returned source URLs: {string.Join(", ", urls)}");
            }
            foreach (CitationAnnotation citation in content.Annotations?.OfType<CitationAnnotation>() ?? [])
            {
                if (citation.Url is not null)
                {
                    sources.Add(citation.Url.ToString());
                }
            }
        }
        // B. BackgroundAgentsProvider returns worker text, so retain real citation metadata there.
        if (sources.Count > 0)
        {
            string sourceText = "Source URLs returned by hosted search metadata:\n" + string.Join("\n", sources.Order());
            transcript.Write("NewsAgent", "SOURCES", sourceText);
            response.Messages.Add(new ChatMessage(ChatRole.Assistant, sourceText));
        }
        // C. Adapter support varies; never turn a text-only claim into a synthetic search event.
        if (!observedSearch && sources.Count == 0)
        {
            transcript.Write("NewsAgent", "SEARCH_EVIDENCE_UNAVAILABLE",
                "No hosted-search events or source metadata returned. Search execution is not verified.");
        }
        return response;
    }
}
