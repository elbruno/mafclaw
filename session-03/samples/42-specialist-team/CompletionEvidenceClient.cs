// Objective: verify specialist completion from SDK outcomes, never from narrative.
// A. Track actual start and result-collection function traffic.
// B. Inspect terminal statuses with the SDK's real read-only task-list tool.
// C. Compare collected results against successful SDK output and required roles.

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample42;

public sealed class CompletionEvidenceClient(IChatClient inner, Transcript transcript) : DelegatingChatClient(inner)
{
    private readonly Dictionary<string, FunctionCallContent> _calls = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seenResults = new(StringComparer.Ordinal);
    private readonly Dictionary<int, string> _started = [];
    private readonly Dictionary<int, List<string>> _collected = [];
    private AIFunction? _listTasks;
    private AIFunction? _getResult;
    private bool _invalidTraffic;

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var history = messages.ToArray();
        Observe(history);
        foreach (AIFunction function in options?.Tools?.OfType<AIFunction>() ?? [])
        {
            if (function.Name == "background_agents_get_all_tasks")
            {
                _listTasks = function;
            }
            if (function.Name == "background_agents_get_task_results")
            {
                _getResult = function;
            }
        }
        var response = await base.GetResponseAsync(history, options, cancellationToken);
        Observe(response.Messages);
        return response;
    }

    public void Observe(IEnumerable<ChatMessage> messages)
    {
        foreach (AIContent content in messages.SelectMany(message => message.Contents))
        {
            if (content is FunctionCallContent call)
            {
                _calls.TryAdd(call.CallId, call);
            }
            if (content is not FunctionResultContent result || !_seenResults.Add(result.CallId) ||
                !_calls.TryGetValue(result.CallId, out FunctionCallContent? request))
            {
                continue;
            }
            string text = result.Result?.ToString() ?? "";
            if (request.Name == "background_agents_start_task")
            {
                Match match = Regex.Match(text, @"^Background task (\d+) started on agent '([^']+)'\.$");
                if (!match.Success || !_started.TryAdd(
                    int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), match.Groups[2].Value))
                {
                    _invalidTraffic = true;
                }
            }
            if (request.Name == "background_agents_get_task_results")
            {
                if (request.Arguments?.TryGetValue("taskId", out object? value) != true ||
                    !JsonSerializer.SerializeToElement(value).TryGetInt32(out int taskId))
                {
                    _invalidTraffic = true;
                    continue;
                }
                if (!_collected.TryGetValue(taskId, out List<string>? values))
                {
                    values = [];
                    _collected.Add(taskId, values);
                }
                values.Add(text);
            }
        }
    }

    public async Task<bool> VerifyAsync(string prompt, CancellationToken cancellationToken)
    {
        if (_listTasks is null || _getResult is null || _invalidTraffic)
        {
            return Reject("SDK evidence is missing or a delegation call failed.");
        }
        // B. These are actual session-bound MAF AIFunctions, not custom background tools.
        // GetIncompleteTasks excludes Failed/Lost; the SDK list tool includes every status.
        string statuses = (await _listTasks.InvokeAsync(new AIFunctionArguments(), cancellationToken))?.ToString() ?? "";
        var tasks = Regex.Matches(statuses, @"(?m)^- Task (\d+) \[([^\]]+)\] \(([^)]+)\):")
            .Select(match => (
                Id: int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                Status: match.Groups[2].Value,
                Agent: match.Groups[3].Value)).ToArray();
        transcript.Write("HOST", "TASK_STATUS",
            tasks.Length == 0 ? "No SDK background tasks." : string.Join(", ", tasks.Select(task => $"{task.Agent}={task.Status}")));
        if (tasks.Length != _started.Count ||
            _started.Values.Distinct(StringComparer.Ordinal).Count() != _started.Count ||
            tasks.Any(task => task.Status != "Completed" ||
                !_started.TryGetValue(task.Id, out string? agent) || agent != task.Agent))
        {
            return Reject("A selected task is Running, Failed, Lost, duplicated, cleared, or missing.");
        }
        string[]? required = ScenarioPrompts.RequiredWorkers(prompt);
        if (required is not null && !required.Order().SequenceEqual(_started.Values.Order()))
        {
            return Reject("Actual dispatched roles do not match this documented scenario.");
        }
        foreach (var task in tasks)
        {
            if (!_collected.TryGetValue(task.Id, out List<string>? collected))
            {
                return Reject("The main agent did not collect every selected worker result.");
            }
            string actual = (await _getResult.InvokeAsync(
                new AIFunctionArguments { ["taskId"] = task.Id }, cancellationToken))?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(actual) || !collected.Contains(actual, StringComparer.Ordinal))
            {
                return Reject("Collected evidence is missing or does not match the successful SDK result.");
            }
        }
        // C. Host verification reads do not count as main-agent collection.
        transcript.Write("HOST", "VERIFIED_RESULTS", $"Verified {_started.Count} dispatched, successful, collected specialist results.");
        return true;
    }

    private bool Reject(string detail)
    {
        transcript.Write("HOST", "UNVERIFIED_RESULTS", detail);
        return false;
    }
}
