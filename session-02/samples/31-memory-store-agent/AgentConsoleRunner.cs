// Session flow:
// A. Create one agent session for the console conversation.
// B. Send each user prompt to the Harness agent.
// C. Print responses and handle any approval requests.
// D. Flush asynchronous Foundry memory updates before exit.

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;

internal static class AgentConsoleRunner
{
    public static async Task RunAsync(AIAgent agent, FoundryMemoryProvider? foundryMemory = null)
    {
        // Reuse one session so the agent keeps turn context during the demo.
        var session = await agent.CreateSessionAsync();

        while (true)
        {
            // Read one prompt from the console and let /exit end the sample.
            Console.Write("> ");
            var input = Console.ReadLine();
            if (input is null || input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            // Send the prompt to Harness, then handle normal text or approvals.
            var response = await agent.RunAsync(input, session);
            await WriteResponseAndHandleApprovalsAsync(agent, session, response);
        }

        if (foundryMemory is not null)
        {
            // Foundry extraction runs in the background; flush it before a restart.
            try
            {
                Console.WriteLine("Waiting for Foundry memory updates to finish...");
                await foundryMemory.WhenUpdatesCompletedAsync();
                Console.WriteLine("Foundry memory updates complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Foundry memory update did not complete cleanly: {ex.Message}");
            }
        }
    }

    private static async Task WriteResponseAndHandleApprovalsAsync(AIAgent agent, AgentSession session, AgentResponse response)
    {
        while (true)
        {
            // Print assistant text first so the audience sees the model response.
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                Console.WriteLine(response.Text);
            }

            // Harness returns approval requests as content in the response.
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
                // Show the tool name and arguments before asking the user.
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

                // Convert the console decision into the Harness approval response.
                approvalResponses.Add(request.CreateResponse(
                    approved,
                    approved ? "Approved by console user." : "Denied by console user."));
            }

            // Continue the same run with the approval answers attached.
            response = await agent.RunAsync([new ChatMessage(ChatRole.User, approvalResponses)], session);
        }
    }
}
