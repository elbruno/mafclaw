using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Checkpoint04;

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

        await output.WriteLineAsync("LIVE · mafclaw · checkpoint 04 · planning and todos");
        await output.WriteLineAsync(
            "Mode starts in plan. Commands: /mode [plan|execute], /todos, /exit");

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
                if (await HandleCommandAsync(
                        trimmed,
                        session,
                        output,
                        cancellationToken))
                {
                    break;
                }

                continue;
            }

            await HandlePromptAsync(
                trimmed,
                session,
                input,
                output,
                cancellationToken);
        }
    }

    private async Task<bool> HandleCommandAsync(
        string command,
        AgentSession session,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var segments = command.Split(
            ' ',
            2,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (string.Equals(segments[0], "/exit", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync("Exiting.");
            return true;
        }

        if (string.Equals(segments[0], "/mode", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length == 1)
            {
                await output.WriteLineAsync($"Current mode: {_mode}");
                return false;
            }

            if (!string.Equals(segments[1], PlanMode, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(segments[1], ExecuteMode, StringComparison.OrdinalIgnoreCase))
            {
                await output.WriteLineAsync("Usage: /mode plan|execute");
                return false;
            }

            _mode = segments[1].ToLowerInvariant();
            await output.WriteLineAsync($"Mode set to {_mode}");
            return false;
        }

        if (string.Equals(segments[0], "/todos", StringComparison.OrdinalIgnoreCase))
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
                await output.WriteLineAsync(
                    $"[{status}] #{todo.Id}: {todo.Title} — {todo.Description}");
            }

            return false;
        }

        await output.WriteLineAsync(
            "Unknown command. Supported commands: /mode, /todos, /exit");
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
            await RunPlanningTurnAsync(
                prompt,
                session,
                input,
                output,
                cancellationToken);
            return;
        }

        var response = await _agent.RunAsync(
            prompt,
            session,
            new AgentRunOptions(),
            cancellationToken);

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
                "Returns clarification questions or an approval request before execution.")
        };

        var planningResult = await _agent.RunAsync<PlanningResponse>(
            prompt,
            session,
            _jsonOptions,
            options,
            cancellationToken);

        var planning = planningResult.Result ??
                       throw new InvalidOperationException(
                           "The planning response was empty.");

        if (planning.Type == PlanningResponseType.Clarification)
        {
            await output.WriteLineAsync("Clarification required:");
            for (var index = 0; index < planning.Questions.Count; index++)
            {
                var question = planning.Questions[index];
                await output.WriteLineAsync($"{index + 1}. {question.Message}");
                if (question.Choices is { Count: > 0 })
                {
                    await output.WriteLineAsync(
                        $"   Choices: {string.Join(", ", question.Choices)}");
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

        var approved = IsApprovalGranted(await input.ReadLineAsync());
        if (!PlanApprovalGate.CanExecute(planning, approved))
        {
            await output.WriteLineAsync(
                "Plan was not approved. Remaining in plan mode.");
            return;
        }

        _mode = ExecuteMode;
        await output.WriteLineAsync("Plan approved. Switched to execute mode.");

        var executionPrompt =
            $"The user approved this plan. Execute now. Original request: {prompt}";

        var executionResult = await _agent.RunAsync(
            executionPrompt,
            session,
            new AgentRunOptions(),
            cancellationToken);

        await WriteResponseAsync(executionResult, output);
    }

    private static bool IsApprovalGranted(string? input) =>
        string.Equals(input?.Trim(), "y", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(input?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);

    private static async Task WriteResponseAsync(
        AgentResponse response,
        TextWriter output)
    {
        await output.WriteLineAsync(
            string.IsNullOrWhiteSpace(response.Text)
                ? "No assistant text response was returned."
                : response.Text);

        var webSearchUsed = response.Messages
            .SelectMany(message => message.Contents)
            .Any(content => content is WebSearchToolCallContent or WebSearchToolResultContent);

        if (webSearchUsed)
        {
            await output.WriteLineAsync("[Hosted web search was used.]");
        }
    }
}
