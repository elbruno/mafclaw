// Objective: run an agent conversation and make each tool approval visible.
// Steps:
// A. Create one session and accept prompts.
// B. Print responses and collect approval requests.
// C. Send each approval decision back to the agent.

// Session flow:
// A. Create one agent session for the console conversation.
// B. Send each user prompt to the Harness agent.
// C. Print responses and stop at any approval request.
// D. Send the user's approval decision back to continue the run.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal static class AgentConsoleRunner
{
    public static async Task RunAsync(AIAgent agent)
    {
        // A. Keep the approval conversation in one session.
        var session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (input is null || input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (input.Trim().StartsWith("/mode ", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"(mode switching is not wired in this sample; staying in execute mode)");
                continue;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            // B. Run the prompt, then inspect response content for approvals.
            var response = await agent.RunAsync(input, session);
            await WriteResponseAndHandleApprovalsAsync(agent, session, response);
        }
    }

    private static async Task WriteResponseAndHandleApprovalsAsync(AIAgent agent, AgentSession session, AgentResponse response)
    {
        while (true)
        {
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                Console.WriteLine(response.Text);
            }

            var approvalRequests = response.Messages
                .SelectMany(message => message.Contents)
                .OfType<ToolApprovalRequestContent>()
                .ToList();

            if (approvalRequests.Count == 0)
            {
                return;
            }

            var approvalResponses = new List<AIContent>();
            foreach (var request in approvalRequests)
            {
                var functionCall = request.ToolCall as FunctionCallContent;
                var toolName = functionCall?.Name ?? request.ToolCall.CallId;
                var arguments = functionCall?.Arguments is null
                    ? string.Empty
                    : string.Join(", ", functionCall.Arguments.Select(item => $"{item.Key}: {item.Value}"));

                Console.Write($"Approve tool {toolName}({arguments})? [y/N]: ");
                var input = Console.ReadLine();
                var approved = input is not null &&
                    (input.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                     input.Equals("yes", StringComparison.OrdinalIgnoreCase));

                approvalResponses.Add(request.CreateResponse(
                    approved,
                    approved ? "Approved by console user." : "Denied by console user."));
            }

            response = await agent.RunAsync([new ChatMessage(ChatRole.User, approvalResponses)], session);
        }
    }
}
