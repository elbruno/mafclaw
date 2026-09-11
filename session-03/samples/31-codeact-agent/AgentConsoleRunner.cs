// Objective: run the CodeAct agent conversation and surface execution approvals.
// Steps:
// A. Create a session and accept prompts.
// B. Display responses and pending tool requests.
// C. Send explicit approval or denial responses.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal static class AgentConsoleRunner
{
    public static async Task RunAsync(AIAgent agent)
    {
        // A. AIAgent.CreateSessionAsync gives MAF-owned conversation state, so
        // this sample does not implement its own message-history container.
        var session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (input is null || input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            // B. AIAgent.RunAsync executes the framework agent loop for one prompt.
            var response = await agent.RunAsync(input, session);
            while (true)
            {
                if (!string.IsNullOrWhiteSpace(response.Text))
                {
                    Console.WriteLine(response.Text);
                }

                var requests = response.Messages
                    .SelectMany(message => message.Contents)
                    .OfType<ToolApprovalRequestContent>()
                    .ToList();

                if (requests.Count == 0)
                {
                    break;
                }

                // C. Send explicit approval or denial for each execution.
                // C. ToolApprovalRequestContent maps the console choice to MAF's
                // approval response instead of a custom tool-call protocol.
                var approvals = new List<AIContent>();
                foreach (var request in requests)
                {
                    var call = request.ToolCall as FunctionCallContent;
                    Console.Write($"Approve skill tool {call?.Name ?? request.ToolCall.CallId}? [y/N]: ");
                    var answer = Console.ReadLine();
                    var approved = answer is not null &&
                        (answer.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                         answer.Equals("yes", StringComparison.OrdinalIgnoreCase));

                    approvals.Add(request.CreateResponse(
                        approved,
                        approved ? "Approved by console user." : "Denied by console user."));
                }

                response = await agent.RunAsync([new ChatMessage(ChatRole.User, approvals)], session);
            }
        }
    }
}
