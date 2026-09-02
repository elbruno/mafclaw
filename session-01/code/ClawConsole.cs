using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Session01;

internal sealed class ClawConsole(
    AIAgent agent,
    TodoProvider todoProvider)
{
    private const string PlanMode = "plan";
    private const string ExecuteMode = "execute";

    private readonly AIAgent _agent = agent;
    private readonly TodoProvider _todoProvider = todoProvider;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private string _mode = PlanMode;

    public async Task RunAsync(
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var session = await _agent.CreateSessionAsync(cancellationToken);

        await output.WriteLineAsync("mafclaw · Session 01");
        await output.WriteLineAsync("Mode starts in plan. Commands: /mode [plan|execute], /todos, /exit");

        while (true)
        {
            await output.WriteAsync("claw> ");
            await output.FlushAsync();

            var line = await input.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                var shouldExit = await HandleCommandAsync(trimmed, session, output, cancellationToken);
                if (shouldExit)
                {
                    break;
                }

                continue;
            }

            await HandlePromptAsync(trimmed, session, input, output, cancellationToken);
        }
    }

    private async Task<bool> HandleCommandAsync(
        string command,
        AgentSession session,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var segments = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var commandName = segments[0];

        if (string.Equals(commandName, "/exit", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync("Exiting.");
            return true;
        }

        if (string.Equals(commandName, "/mode", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length == 1)
            {
                await output.WriteLineAsync($"Current mode: {_mode}");
                return false;
            }

            var requestedMode = segments[1];
            if (!string.Equals(requestedMode, PlanMode, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(requestedMode, ExecuteMode, StringComparison.OrdinalIgnoreCase))
            {
                await output.WriteLineAsync("Usage: /mode plan|execute");
                return false;
            }

            _mode = requestedMode.ToLowerInvariant();
            await output.WriteLineAsync($"Mode set to {_mode}");
            return false;
        }

        if (string.Equals(commandName, "/todos", StringComparison.OrdinalIgnoreCase))
        {
            var todos = await _todoProvider.GetAllTodosAsync(session, cancellationToken);
            if (todos.Count == 0)
            {
                await output.WriteLineAsync("No todos yet.");
                return false;
            }

            foreach (var todo in todos.OrderBy(item => item.Id))
            {
                var status = todo.IsComplete ? "x" : " ";
                await output.WriteLineAsync($"[{status}] #{todo.Id}: {todo.Title} — {todo.Description}");
            }

            return false;
        }

        await output.WriteLineAsync("Unknown command. Supported commands: /mode, /todos, /exit");
        return false;
    }

    private async Task HandlePromptAsync(
        string prompt,
        AgentSession session,
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        if (string.Equals(_mode, PlanMode, StringComparison.OrdinalIgnoreCase))
        {
            await RunPlanningTurnAsync(prompt, session, input, output, cancellationToken);
            return;
        }

        var response = await _agent.RunAsync(prompt, session, new AgentRunOptions(), cancellationToken);
        await WriteResponseAsync(response, output);
    }

    private async Task RunPlanningTurnAsync(
        string prompt,
        AgentSession session,
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var options = new AgentRunOptions
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema<PlanningResponse>(
                _jsonOptions,
                "planning_response",
                "Returns clarification questions or approval request before execution.")
        };

        var planningResult = await _agent.RunAsync<PlanningResponse>(
            prompt,
            session,
            _jsonOptions,
            options,
            cancellationToken);

        var planning = planningResult.Result ??
                       throw new InvalidOperationException("Planning response was empty.");

        if (planning.Type == PlanningResponseType.Clarification)
        {
            await output.WriteLineAsync("Clarification required:");
            for (var index = 0; index < planning.Questions.Count; index++)
            {
                var question = planning.Questions[index];
                await output.WriteLineAsync($"{index + 1}. {question.Message}");
                if (question.Choices is { Count: > 0 })
                {
                    await output.WriteLineAsync($"   Choices: {string.Join(", ", question.Choices)}");
                }
            }

            return;
        }

        var summary = planning.Questions.Count > 0
            ? planning.Questions[0].Message
            : "No plan summary was provided.";

        await output.WriteLineAsync("Plan approval required:");
        await output.WriteLineAsync(summary);
        await output.WriteAsync("Approve plan? (y/n): ");
        await output.FlushAsync();

        var approvalInput = await input.ReadLineAsync();
        var isApproved = IsApprovalGranted(approvalInput);

        var shouldExecute = false;
        PlanApprovalGate.TryExecute(planning, isApproved, () => shouldExecute = true);

        if (!shouldExecute)
        {
            await output.WriteLineAsync("Plan was not approved. Remaining in plan mode.");
            return;
        }

        _mode = ExecuteMode;
        await output.WriteLineAsync("Plan approved. Switched to execute mode.");

        var executionPrompt = $"The user approved this plan. Execute now. Original request: {prompt}";
        var executionResult = await _agent.RunAsync(
            executionPrompt,
            session,
            new AgentRunOptions(),
            cancellationToken);

        await WriteResponseAsync(executionResult, output);
    }

    private static bool IsApprovalGranted(string? approvalInput)
    {
        return string.Equals(approvalInput?.Trim(), "y", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(approvalInput?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteResponseAsync(AgentResponse response, TextWriter output)
    {
        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            await output.WriteLineAsync(response.Text);
        }
        else
        {
            await output.WriteLineAsync("No assistant text response was returned.");
        }

        var webSearchUsed = response.Messages
            .SelectMany(message => message.Contents)
            .Any(content => content is WebSearchToolCallContent || content is WebSearchToolResultContent);

        if (webSearchUsed)
        {
            await output.WriteLineAsync("[Hosted web search was used by the model response.]");
        }
    }
}
