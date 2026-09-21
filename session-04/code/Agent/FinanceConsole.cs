// Objective: show real results and bind explicit decisions to pending tool calls.
// A. Create a tracked MAF session and handle local inspection commands.
// B. Run each turn with a deadline and print actual tool results.
// C. Return approval/denial through MAF's response protocol.
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04;

public static class FinanceConsole
{
    public static async Task RunAsync(FinanceAgentBuild build, CancellationToken cancellationToken = default)
    {
        var session = await build.CreateSessionAsync(cancellationToken);
        Console.WriteLine("Commands: /todos, /memory, /exit. All data is synthetic; not financial advice.");
        while (true)
        {
            Console.Write("> ");
            var input = await Console.In.ReadLineAsync(cancellationToken);
            if (input is null || input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase)) return;
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Trim() == "/todos")
            {
                var provider = build.Agent.GetService<TodoProvider>();
                if (provider is null) { Console.WriteLine("Todo provider is disabled in this profile."); continue; }
                var todos = await provider.GetAllTodosAsync(session, cancellationToken);
                if (todos.Count == 0) Console.WriteLine("No todos yet.");
                foreach (var todo in todos) Console.WriteLine($"[{(todo.IsComplete ? "x" : " ")}] {todo.Title}");
                continue;
            }
            if (input.Trim() == "/memory")
            {
                if (build.MemoryMode != "file")
                {
                    Console.WriteLine(build.MemoryMode == "foundry"
                        ? "Foundry-managed memory: ask a recall question; there is no local file receipt."
                        : "Memory is disabled in this profile.");
                    continue;
                }
                var root = Path.Combine(build.WorkingDirectory, "memory", "current-user", "facts");
                if (!Directory.Exists(root)) { Console.WriteLine("No current-user memory files yet."); continue; }
                if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Memory inspection refuses a linked directory.");
                foreach (var file in Directory.EnumerateFiles(root))
                    Console.WriteLine($"Memory file: {Path.GetFileName(file)} ({new FileInfo(file).Length} bytes)");
                continue;
            }
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(120));
            var response = await build.Agent.RunAsync(input, session, cancellationToken: deadline.Token);
            for (var round = 0; ; round++)
            {
                PrintResponse(response);
                var requests = response.Messages.SelectMany(message => message.Contents)
                    .OfType<ToolApprovalRequestContent>().ToArray();
                if (requests.Length == 0) break;
                if (round >= 12) throw new InvalidOperationException("Approval round limit reached.");
                var decisions = new List<AIContent>();
                foreach (var request in requests)
                {
                    var call = request.ToolCall as FunctionCallContent;
                    Console.WriteLine($"Proposed tool: {call?.Name ?? "tool"}");
                    if (call?.Arguments is not null) Console.WriteLine(JsonSerializer.Serialize(call.Arguments));
                    Console.Write("Approve this exact operation? [y/N]: ");
                    var answer = await Console.In.ReadLineAsync(deadline.Token);
                    var approved = string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase);
                    decisions.Add(request.CreateResponse(approved, approved ? "Approved by user." : "Denied by user."));
                }
                response = await build.Agent.RunAsync([new ChatMessage(ChatRole.User, decisions)], session,
                    cancellationToken: deadline.Token);
            }
        }
    }

    public static void PrintResponse(AgentResponse response)
    {
        foreach (var result in response.Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>())
            Console.WriteLine($"TOOL RESULT [{result.CallId}]: {JsonSerializer.Serialize(result.Result)}");
        if (!string.IsNullOrWhiteSpace(response.Text)) Console.WriteLine(response.Text);
    }
}
