// Objective: inspect real provider capabilities instead of inventing tool contracts.
// A. Inject a non-network chat client into the actual Harness.
// B. Capture the SDK-generated AIFunction schemas on an invocation.
// C. Release the session through the same owning provider.

using MafClaw.OrchestrationSupport;
using MafClaw.Sample42;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.SpecialistSamples.Tests;

public static class ProviderContractTests
{
    public static async Task RunAsync()
    {
        int workerCalls = 0, round = 0;
        var results = new List<string>();
        var tools = new Dictionary<string, AIFunction>(StringComparer.Ordinal);
        using var workerClient = new FixtureChatClient((_, _, _) =>
        {
            Interlocked.Increment(ref workerCalls);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Actual child evidence.")));
        });
        using var client = new FixtureChatClient((messages, options, _) =>
        {
            foreach (AIFunction function in options?.Tools?.OfType<AIFunction>() ?? [])
            {
                tools[function.Name] = function;
            }
            if (round++ % 2 == 0)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [
                    new FunctionCallContent($"start-{round}", "background_agents_start_task", new Dictionary<string, object?>
                    {
                        ["agentName"] = "NewsAgent", ["input"] = "Educational task", ["description"] = "News"
                    })
                ])));
            }
            results.Add(messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Last().Result!.ToString()!);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Inspection complete.")));
        });
        var transcript = new Transcript(TextWriter.Null);
        var worker = workerClient.AsAIAgent(name: "NewsAgent", description: "News only.");
        var provider = new BackgroundAgentsProvider([worker], new BackgroundAgentsProviderOptions());
        var agent = AgentFactory.CreateMain(client, provider, transcript, new RunLimits());
        var session = await agent.CreateSessionAsync();
        try
        {
            await agent.RunAsync("Inspect capabilities.", session);
            string[] expected =
            [
                "background_agents_start_task", "background_agents_wait_for_first_completion",
                "background_agents_get_task_results", "background_agents_get_all_tasks",
                "background_agents_continue_task", "background_agents_clear_completed_task"
            ];
            Check.True(tools.Keys.Order().SequenceEqual(expected.Order()), "Only six real background tools; no defaults or duplicates.");
            string[] startProperties = tools["background_agents_start_task"].JsonSchema
                .GetProperty("properties").EnumerateObject().Select(property => property.Name).ToArray();
            Check.True(startProperties.Order().SequenceEqual(new[] { "agentName", "input", "description" }.Order()),
                "SDK start parameter contract changed.");
            Check.True(results[0].Contains("Background task 1 started", StringComparison.Ordinal), "Real provider start result.");
        }
        finally
        {
            await provider.ReleaseSessionAsync(session, cancelRunning: true, timeout: TimeSpan.FromSeconds(5));
        }
        int callsBeforeReleasedRetry = workerCalls;
        await agent.RunAsync("A released session must refuse another task.", session);
        Check.Equal(callsBeforeReleasedRetry, workerCalls, "Terminal release must prevent another child session.");
        Check.True(results[^1].Contains("released", StringComparison.OrdinalIgnoreCase), "SDK must explain terminal release.");
        Check.Equal(0, provider.GetIncompleteTasks(session).Count, "Release must clear all running tasks.");
        await provider.ReleaseSessionAsync(session, cancelRunning: true, timeout: TimeSpan.FromSeconds(5));
    }
}
