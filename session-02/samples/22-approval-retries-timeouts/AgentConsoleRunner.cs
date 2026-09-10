// Session flow:
// A. Create one agent session for the console conversation.
// B. Send each prompt to the Harness agent.
// C. Route approval requests through the timeout/retry policy.
// D. Return the final decision so the agent can continue safely.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal static class AgentConsoleRunner
{
    private const int MaxApprovalRoundsPerPrompt = 5;

    public static async Task RunAsync(AIAgent agent, TimedApprovalPolicy approvalPolicy)
    {
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

            var response = await agent.RunAsync(input, session);
            await WriteResponseAndHandleApprovalsAsync(agent, session, response, approvalPolicy);
        }
    }

    private static async Task WriteResponseAndHandleApprovalsAsync(
        AIAgent agent,
        AgentSession session,
        AgentResponse response,
        TimedApprovalPolicy approvalPolicy)
    {
        var approvalRound = 0;
        var approvalDeniedForPrompt = false;

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

            approvalRound++;
            if (approvalRound > MaxApprovalRoundsPerPrompt)
            {
                Console.WriteLine("Approval denied: the agent exceeded the approval round limit for this prompt.");
                return;
            }

            var approvalResponses = new List<AIContent>();
            foreach (var request in approvalRequests)
            {
                // Show exactly what the model requested before applying the reliability policy.
                var functionCall = request.ToolCall as FunctionCallContent;
                var toolName = functionCall?.Name ?? request.ToolCall.CallId;
                var arguments = functionCall?.Arguments is null
                    ? string.Empty
                    : string.Join(", ", functionCall.Arguments.Select(item => $"{item.Key}: {item.Value}"));

                var decision = approvalDeniedForPrompt
                    ? new ApprovalDecision(false, "Denied automatically because another approval was already denied for this prompt.")
                    : await approvalPolicy.RequestApprovalAsync(toolName, arguments);

                approvalDeniedForPrompt |= !decision.Approved;
                approvalResponses.Add(request.CreateResponse(decision.Approved, decision.Message));
                Console.WriteLine(decision.Message);
            }

            response = await agent.RunAsync([new ChatMessage(ChatRole.User, approvalResponses)], session);
        }
    }
}
