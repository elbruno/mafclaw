// Objective: preserve service-returned search evidence without performing network calls.
// A. Supply clearly synthetic SDK search/citation content from an offline client.
// B. Run the actual live-capability NewsAgent through MAF.
// C. Assert event separation and source retention in the worker's text result.

using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using S42 = MafClaw.Sample42;
using S43 = MafClaw.Sample43;

namespace MafClaw.SpecialistSamples.Tests;

public static class HostedNewsEvidenceTests
{
    public static async Task RunAsync()
    {
        foreach (int sample in new[] { 42, 43 })
        {
            using var probe = new FixtureChatClient((_, _, _) => Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant,
                [
                    new WebSearchToolCallContent("synthetic-search") { Queries = ["synthetic public news query"] },
                    new WebSearchToolResultContent("synthetic-search")
                    {
                        Outputs = [new UriContent(new Uri("https://example.org/synthetic-result"), "text/html")]
                    },
                    new TextContent("SYNTHETIC OFFLINE EVENT TEST. No search was performed.")
                    {
                        Annotations =
                        [
                            new CitationAnnotation
                            {
                                Url = new Uri("https://example.org/synthetic-citation"),
                                Title = "Synthetic source metadata"
                            }
                        ]
                    }
                ]))));
            var transcript = new Transcript(TextWriter.Null);
            AIAgent worker = sample == 42
                ? S42.AgentFactory.CreateWorker("NewsAgent", probe, new S42.MockData(), transcript, new S42.RunLimits(), fixture: false)
                : S43.AgentFactory.CreateWorker("NewsAgent", probe, new S43.MockData(), transcript, new S43.RunLimits(), fixture: false);
            var session = await worker.CreateSessionAsync();
            var response = await worker.RunAsync("Offline hosted-event adaptation test.", session);
            Check.Equal(1, probe.Calls, "The hosted event probe must not trigger an extra model or network call.");
            Check.Equal(1, transcript.Entries.Count(entry => entry.State == "TOOL_CALL"), "One observed hosted call.");
            Check.Equal(1, transcript.Entries.Count(entry => entry.State == "TOOL_RESULT"), "One observed hosted result.");
            Check.True(response.Text.Contains("https://example.org/synthetic-result", StringComparison.Ordinal) &&
                response.Text.Contains("https://example.org/synthetic-citation", StringComparison.Ordinal),
                "Source URLs must survive the provider's plain-text fan-in.");
            Check.True(transcript.Entries.Any(entry => entry.State == "SOURCES"), "Sources must be separate from narrative.");

            using var liveMarker = new FixtureChatClient((_, _, _) =>
                throw new InvalidOperationException("Every inference client must be supplied by the offline test."));
            string prompt = S43.ScenarioPrompts.News;
            var result = await SampleHarness.RunAsync(sample, prompt,
                clients: name => name == "MainAgent" ? SampleHarness.Script(sample, name, prompt) : probe,
                live: liveMarker);
            Check.True(result.Completed && result.SessionReleased, "The actual provider must collect and release the live-capability news worker.");
            Check.True(result.Narrative.Contains("https://example.org/synthetic-result", StringComparison.Ordinal) &&
                result.Narrative.Contains("https://example.org/synthetic-citation", StringComparison.Ordinal),
                "Both returned sources must survive real BackgroundAgentsProvider fan-in.");
            Check.True(result.Events.Any(entry => entry.Agent == "NewsAgent" && entry.State == "TOOL_CALL" &&
                entry.Detail.StartsWith("hosted_web_search ", StringComparison.Ordinal)),
                "Live TeamRunner wiring must preserve the hosted-search evidence adapter.");
        }
    }
}
