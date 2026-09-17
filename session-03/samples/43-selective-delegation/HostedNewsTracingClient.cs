// Objective: distinguish service-returned search evidence from a model's claims.
// A. Observe actual MAF hosted web-search call and result content.
// B. Retain returned source URLs in the plain-text worker result.
// C. Flag absent search evidence without manufacturing events.

using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample43;

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
            // A. Hosted search happens at the service; these are returned SDK events.
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
        // B. The provider's text-only fan-in must not discard actual citation URLs.
        if (sources.Count > 0)
        {
            string sourceText = "Source URLs returned by hosted search metadata:\n" + string.Join("\n", sources.Order());
            transcript.Write("NewsAgent", "SOURCES", sourceText);
            response.Messages.Add(new ChatMessage(ChatRole.Assistant, sourceText));
        }
        // C. A model's text alone is not proof of service-side tool execution.
        if (!observedSearch && sources.Count == 0)
        {
            transcript.Write("NewsAgent", "SEARCH_EVIDENCE_UNAVAILABLE",
                "No hosted-search events or source metadata returned. Search execution is not verified.");
        }
        return response;
    }
}
