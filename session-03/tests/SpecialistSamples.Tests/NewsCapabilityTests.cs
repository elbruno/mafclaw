// Objective: verify live search and offline fiction have disjoint capabilities.
// A. Construct all workers in both modes with a non-network probe client.
// B. Inspect the actual tool list passed through MAF on invocation.
// C. Keep allocation/risk mock tools unchanged and require source instructions.

using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using S42 = MafClaw.Sample42;
using S43 = MafClaw.Sample43;

namespace MafClaw.SpecialistSamples.Tests;

public static class NewsCapabilityTests
{
    public static async Task RunAsync()
    {
        foreach (int sample in new[] { 42, 43 })
        {
            foreach (bool fixture in new[] { true, false })
            {
                foreach (string name in new[] { "NewsAgent", "AllocationAgent", "RiskAgent" })
                {
                    using var probe = new FixtureChatClient((messages, options, cancellationToken) =>
                    {
                        AITool[] tools = options?.Tools?.ToArray() ?? [];
                        Check.Equal(1, tools.Length, "A worker must have exactly one evidence capability.");
                        if (name == "NewsAgent" && !fixture)
                        {
                            Check.True(tools[0] is HostedWebSearchTool, "Live NewsAgent has only HostedWebSearchTool.");
                            Check.True(!tools.OfType<AIFunction>().Any(), "Live news must not expose read_mock_news or other local tools.");
                            string instructions = (options?.Instructions ?? "") + string.Join("\n", messages.Select(message => message.Text));
                            Check.True(instructions.Contains("source URLs", StringComparison.Ordinal) &&
                                instructions.Contains("publication dates", StringComparison.Ordinal) &&
                                instructions.Contains("public information only", StringComparison.Ordinal),
                                "Live news requires public sources and publication dates.");
                        }
                        else
                        {
                            string expected = name switch
                            {
                                "NewsAgent" => "read_mock_news",
                                "AllocationAgent" => "calculate_mock_allocation",
                                _ => "assess_mock_risk"
                            };
                            Check.True(tools[0] is AIFunction function && function.Name == expected,
                                "Fixture news and all portfolio workers use only their deterministic local tool.");
                            Check.True(!tools.OfType<HostedWebSearchTool>().Any(), "No hosted search outside live NewsAgent.");
                        }
                        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                            "OFFLINE CAPABILITY PROBE ONLY: no public search was executed.")));
                    });
                    var transcript = new Transcript(TextWriter.Null);
                    AIAgent worker = sample == 42
                        ? S42.AgentFactory.CreateWorker(name, probe, new S42.MockData(), transcript, new S42.RunLimits(), fixture)
                        : S43.AgentFactory.CreateWorker(name, probe, new S43.MockData(), transcript, new S43.RunLimits(), fixture);
                    var session = await worker.CreateSessionAsync();
                    await worker.RunAsync("Inspect capabilities without invoking any tool.", session);
                    Check.Equal(1, probe.Calls, "The offline probe replaces inference; no hosted tool is executed.");
                    Check.Equal(0, transcript.Entries.Count(entry => entry.State == "TOOL_CALL"), "Do not fabricate a search event.");
                    if (name == "NewsAgent" && !fixture)
                    {
                        Check.True(transcript.Entries.Any(entry => entry.State == "SEARCH_EVIDENCE_UNAVAILABLE"),
                            "Text-only probe must explicitly report missing hosted evidence.");
                    }
                }
            }
        }
    }
}
