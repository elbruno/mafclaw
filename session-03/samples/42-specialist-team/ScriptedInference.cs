// Objective: replace only inference with a clearly labelled offline classroom script.
// A. Emit real SDK function calls for the exact documented scenarios.
// B. Read task IDs, statuses and results returned by BackgroundAgentsProvider.
// C. Synthesize only after real read-only tools and fan-in have completed.

using System.Globalization;
using System.Text.RegularExpressions;
using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample42;

public sealed class ScriptedInference(string agentName, string prompt)
{
    private readonly string[] _workers = agentName == "MainAgent" ? ScenarioPrompts.ScriptedWorkers(prompt) : [];
    private readonly Dictionary<string, int> _taskIds = new(StringComparer.Ordinal);
    private int _stage;
    private int _sequence;

    public IChatClient CreateClient() => new FixtureChatClient(RespondAsync);

    private Task<ChatResponse> RespondAsync(
        IReadOnlyList<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ChatResponse response = agentName == "MainAgent"
            ? Coordinate(messages, options)
            : Work(messages, options);
        return Task.FromResult(response);
    }

    private ChatResponse Work(IReadOnlyList<ChatMessage> messages, ChatOptions? options)
    {
        if (_stage++ == 0)
        {
            // A. The actual Harness supplies the one permitted AIFunction.
            var tool = options?.Tools?.OfType<AIFunction>().Single()
                ?? throw new InvalidOperationException("Expected the specialist's one read-only tool.");
            return Call(options, tool.Name, []);
        }
        string result = Result(messages, $"{agentName}-1");
        return Text($"SCRIPTED FIXTURE {agentName} advisory evidence (not live inference):\n{result}");
    }

    private ChatResponse Coordinate(IReadOnlyList<ChatMessage> messages, ChatOptions? options)
    {
        if (_workers.Length == 0)
        {
            return Text("SCRIPTED FIXTURE explanation: diversification spreads exposure across assets. " +
                "No worker or data lookup was needed. Educational, mock holdings, not financial advice.");
        }
        if (_stage == 0)
        {
            _stage = 1;
            return Calls(options, _workers.Select(worker => (
                "background_agents_start_task",
                new Dictionary<string, object?>
                {
                    ["agentName"] = worker,
                    ["input"] = $"Return your specific educational evidence for this request: {prompt}",
                    ["description"] = $"{worker} educational evidence"
                })).ToArray());
        }
        if (_stage == 1)
        {
            // B. IDs come from SDK start results, never from assumed ticket numbers.
            for (int index = 0; index < _workers.Length; index++)
            {
                string result = Result(messages, $"MainAgent-{index + 1}");
                Match match = Regex.Match(result, @"^Background task (\d+) started on agent '");
                if (!match.Success)
                {
                    throw new InvalidOperationException("The real provider did not return a started task.");
                }
                _taskIds[_workers[index]] = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            _stage = 2;
            return Wait(options, _taskIds.Values);
        }
        if (_stage == 2)
        {
            _stage = 3;
            return Call(options, "background_agents_get_all_tasks", []);
        }
        if (_stage == 3)
        {
            string statuses = Result(messages, $"MainAgent-{_sequence}");
            int[] pending = _taskIds.Values.Where(id =>
                statuses.Contains($"Task {id} [Running]", StringComparison.Ordinal)).ToArray();
            if (pending.Length > 0)
            {
                _stage = 2;
                return Wait(options, pending);
            }
            if (_taskIds.Values.Any(id => !statuses.Contains($"Task {id} [Completed]", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("A specialist did not complete successfully.");
            }
            _stage = 4;
            return Calls(options, _taskIds.Values.Select(id => (
                "background_agents_get_task_results",
                new Dictionary<string, object?> { ["taskId"] = id })).ToArray());
        }
        // C. Every collected value is the SDK's real worker output, including real mock-tool math.
        string[] evidence = Enumerable.Range(_sequence - _workers.Length + 1, _workers.Length)
            .Select(sequence => Result(messages, $"MainAgent-{sequence}")).ToArray();
        return Text("SCRIPTED FIXTURE MAIN NARRATIVE — educational, mock holdings, not financial advice.\n" +
            string.Join("\n\n", evidence));
    }

    private ChatResponse Wait(ChatOptions? options, IEnumerable<int> taskIds) =>
        Call(options, "background_agents_wait_for_first_completion",
            new Dictionary<string, object?> { ["taskIds"] = taskIds.ToArray() });

    private ChatResponse Call(ChatOptions? options, string name, Dictionary<string, object?> arguments) =>
        Calls(options, [(name, arguments)]);

    private ChatResponse Calls(ChatOptions? options, (string Name, Dictionary<string, object?> Arguments)[] calls)
    {
        var contents = new List<AIContent>();
        foreach (var call in calls)
        {
            var function = options?.Tools?.OfType<AIFunction>().SingleOrDefault(tool => tool.Name == call.Name)
                ?? throw new InvalidOperationException("SDK tool unavailable or host iteration budget exhausted.");
            // Validate the captured SDK schema so a future contract change fails visibly.
            string[] required = function.JsonSchema.TryGetProperty("required", out var requiredProperties)
                ? requiredProperties.EnumerateArray().Select(item => item.GetString()!).ToArray()
                : [];
            if (required.Any(name => !call.Arguments.ContainsKey(name)) ||
                call.Arguments.Keys.Any(name => !function.JsonSchema.GetProperty("properties").TryGetProperty(name, out _)))
            {
                throw new InvalidOperationException("Scripted arguments do not match the installed SDK schema.");
            }
            contents.Add(new FunctionCallContent($"{agentName}-{++_sequence}", call.Name, call.Arguments));
        }
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, contents));
    }

    private static string Result(IReadOnlyList<ChatMessage> messages, string callId) =>
        messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>()
            .LastOrDefault(result => result.CallId == callId)?.Result?.ToString()
        ?? throw new InvalidOperationException("Expected an actual tool result before scripted synthesis.");

    private static ChatResponse Text(string text) => new(new ChatMessage(ChatRole.Assistant, text));
}
