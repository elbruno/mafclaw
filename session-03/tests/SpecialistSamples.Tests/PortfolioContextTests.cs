// Objective: keep the live classroom demo scoped to its bundled mock portfolio.
// Steps:
// A. Capture the actual instructions MAF supplies to each main agent's client.
// B. Verify both coordinators identify the available fixture and reject real-data solicitation.
// C. Release the provider session without a cloud call.

using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.SpecialistSamples.Tests;

internal static class PortfolioContextTests
{
    public static async Task RunAsync()
    {
        Func<IChatClient, BackgroundAgentsProvider, AIAgent>[] builders =
        [
            (client, provider) => Sample42.AgentFactory.CreateMain(
                client, provider, new Transcript(TextWriter.Null), new Sample42.RunLimits()),
            (client, provider) => Sample43.AgentFactory.CreateMain(
                client, provider, new Transcript(TextWriter.Null), new Sample43.RunLimits())
        ];
        foreach (var build in builders)
        {
            var instructions = "";
            using var client = new FixtureChatClient((messages, options, _) =>
            {
                instructions = string.Join("\n", messages.Where(message => message.Role == ChatRole.System)
                    .Select(message => message.Text)) + "\n" + options?.Instructions;
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Fixture context check.")));
            });
            var provider = new BackgroundAgentsProvider([client.AsAIAgent(name: "AllocationAgent")]);
            var agent = build(client, provider);
            var session = await agent.CreateSessionAsync();
            try
            {
                await agent.RunAsync("Analyze my mock portfolio.", session);
                if (!instructions.Contains("bundled mock holdings", StringComparison.Ordinal) ||
                    !instructions.Contains("Do not ask for real holdings", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The coordinator lost its mock-data-only classroom context.");
                }
            }
            finally
            {
                await provider.ReleaseSessionAsync(session, cancelRunning: true);
            }
        }
    }
}
