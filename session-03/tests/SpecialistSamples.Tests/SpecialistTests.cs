// Objective: verify genuine multi-specialist work and selective non-dispatch.
// A. Force workers to overlap before allowing their real mock tools to finish.
// B. Check actual function results and deterministic portfolio arithmetic.
// C. Assert live-client injection never passes through the fixture prompt router.

using System.Collections.Concurrent;
using System.Text.Json;
using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;
using Prompts = MafClaw.Sample43.ScenarioPrompts;

namespace MafClaw.SpecialistSamples.Tests;

public static class SpecialistTests
{
    public static async Task Morning42Async() =>
        await CheckScenarioAsync(42, Prompts.MorningBrief, ["NewsAgent", "AllocationAgent", "RiskAgent"]);

    public static async Task Selective43Async()
    {
        await CheckScenarioAsync(43, Prompts.Explanation, []);
        await CheckScenarioAsync(43, Prompts.News, ["NewsAgent"]);
        await CheckScenarioAsync(43, Prompts.Portfolio, ["AllocationAgent", "RiskAgent"]);
        await CheckScenarioAsync(43, Prompts.MorningBrief, ["NewsAgent", "AllocationAgent", "RiskAgent"]);
    }

    private static async Task CheckScenarioAsync(int sample, string prompt, string[] expectedWorkers)
    {
        var requests = new ConcurrentDictionary<string, int>();
        var schemas = new ConcurrentDictionary<string, string[]>();
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int workerStarts = 0;
        IChatClient Factory(string name)
        {
            IChatClient script = SampleHarness.Script(sample, name, prompt);
            return new FixtureChatClient(async (messages, options, cancellationToken) =>
            {
                int count = requests.AddOrUpdate(name, 1, (_, old) => old + 1);
                schemas[name] = options?.Tools?.Select(tool => tool is AIFunction function ? function.Name : tool.GetType().Name)
                    .Order().ToArray() ?? [];
                if (name != "MainAgent" && count == 1)
                {
                    if (Interlocked.Increment(ref workerStarts) == expectedWorkers.Length)
                    {
                        allStarted.TrySetResult();
                    }
                    // A. A sequential fake delegation loop would deadlock here and hit the host deadline.
                    await allStarted.Task.WaitAsync(cancellationToken);
                }
                return await script.GetResponseAsync(messages, options, cancellationToken);
            });
        }
        var result = await SampleHarness.RunAsync(sample, prompt, Factory, deadline: TimeSpan.FromSeconds(5));
        Check.True(result.Completed && result.SessionReleased && result.RunningAfterRelease == 0, "Completed turn and provider cleanup.");
        string[] used = requests.Keys.Where(name => name != "MainAgent").Order().ToArray();
        Check.True(used.SequenceEqual(expectedWorkers.Order()), "No unnecessary workers may execute.");
        foreach (string worker in expectedWorkers)
        {
            string expectedTool = worker switch
            {
                "NewsAgent" => "read_mock_news",
                "AllocationAgent" => "calculate_mock_allocation",
                _ => "assess_mock_risk"
            };
            Check.True(schemas[worker].SequenceEqual([expectedTool]), "Each worker has exactly its read-only tool.");
            Check.Equal(2, requests[worker], "One real tool call then specialist synthesis.");
            Check.True(result.Events.Any(entry => entry.Agent == worker && entry.State == "TOOL_RESULT"),
                "A specialist narrative alone is not tool evidence.");
        }
        int startCount = result.Events.Count(entry => entry.Agent == "MainAgent" && entry.State == "TOOL_CALL" &&
            entry.Detail.StartsWith("background_agents_start_task ", StringComparison.Ordinal));
        int collected = result.Events.Count(entry => entry.Agent == "MainAgent" && entry.State == "TOOL_CALL" &&
            entry.Detail.StartsWith("background_agents_get_task_results ", StringComparison.Ordinal));
        Check.Equal(expectedWorkers.Length, startCount, "Exact real dispatch count.");
        Check.Equal(expectedWorkers.Length, collected, "Every selected worker must be collected.");
        if (expectedWorkers.Length == 0)
        {
            Check.True(!result.Events.Any(entry => entry.State == "TOOL_CALL"), "Explanation must not invoke any tool.");
        }
        else
        {
            Check.True(result.Events.Any(entry => entry.State == "TOOL_CALL" &&
                entry.Detail.StartsWith("background_agents_wait_for_first_completion ", StringComparison.Ordinal)),
                "Use the real provider wait tool.");
            int lastStart = result.Events.ToList().FindLastIndex(entry => entry.State == "TOOL_CALL" &&
                entry.Detail.StartsWith("background_agents_start_task ", StringComparison.Ordinal));
            int firstCollect = result.Events.ToList().FindIndex(entry => entry.State == "TOOL_CALL" &&
                entry.Detail.StartsWith("background_agents_get_task_results ", StringComparison.Ordinal));
            Check.True(lastStart < firstCollect, "Fan out before fan in.");
        }
        // B. Parse real tool outputs, not a static script's expected answer text.
        if (expectedWorkers.Contains("AllocationAgent"))
        {
            using JsonDocument allocation = ReadToolJson(result.Events, "AllocationAgent");
            Check.Equal(100000m, allocation.RootElement.GetProperty("totalValue").GetDecimal(), "Mock total.");
            var weights = allocation.RootElement.GetProperty("allocations").EnumerateArray()
                .ToDictionary(item => item.GetProperty("assetClass").GetString()!, item => item.GetProperty("percent").GetDecimal());
            Check.Equal(65m, weights["Equities"], "Equity weight.");
            Check.Equal(25m, weights["Bonds"], "Bond weight.");
            Check.Equal(10m, weights["Cash"], "Cash weight.");
            Check.True(result.Narrative.Contains("\"totalValue\":100000", StringComparison.Ordinal), "Fan-in preserves actual allocation result.");
        }
        if (expectedWorkers.Contains("RiskAgent"))
        {
            using JsonDocument risk = ReadToolJson(result.Events, "RiskAgent");
            Check.Equal(55m, risk.RootElement.GetProperty("largestHoldingPercent").GetDecimal(), "Largest mock holding.");
            Check.Equal(14250m, risk.RootElement.GetProperty("stressLoss").GetDecimal(), "Deterministic stress loss.");
            Check.Equal(14.25m, risk.RootElement.GetProperty("stressLossPercent").GetDecimal(), "Stress percentage.");
            Check.True(risk.RootElement.GetProperty("concentrationFlag").GetBoolean(), "Concentration threshold.");
        }
        Check.True(result.Narrative.Contains("SCRIPTED FIXTURE", StringComparison.Ordinal), "Offline inference must be labelled.");
    }

    private static JsonDocument ReadToolJson(IReadOnlyList<TranscriptEntry> events, string agent)
    {
        string detail = events.Single(entry => entry.Agent == agent && entry.State == "TOOL_RESULT").Detail;
        string payload = detail[(detail.IndexOf("] ", StringComparison.Ordinal) + 2)..];
        // TracingChatClient may receive a JsonElement containing a JSON-encoded string.
        using JsonDocument envelope = JsonDocument.Parse(payload);
        return JsonDocument.Parse(envelope.RootElement.ValueKind == JsonValueKind.String
            ? envelope.RootElement.GetString()! : envelope.RootElement.GetRawText());
    }

    public static async Task LiveSelectionAsync()
    {
        foreach (int sample in new[] { 42, 43 })
        {
            int calls = 0;
            using var injectedLive = new FixtureChatClient((messages, options, cancellationToken) =>
            {
                Interlocked.Increment(ref calls);
                Check.Equal(6, options!.Tools!.Count, "Main sees the provider's full roster capability.");
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                    "Injected model elected no delegation. Educational, mock holdings, not financial advice.")));
            });
            // C. Deliberately not a supported fixture prompt; the live path must not classify it.
            var result = await SampleHarness.RunAsync(sample, "A novel unlisted question needing no external evidence.", live: injectedLive);
            Check.True(result.Completed, "Live-client injection must accept arbitrary prompts without fixture routing.");
            Check.Equal(1, calls, "The main model alone chose to answer.");
            Check.True(!result.Events.Any(entry => entry.State == "TOOL_CALL"), "No host-triggered background worker.");
        }
    }
}
