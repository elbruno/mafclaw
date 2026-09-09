using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;

internal static class AgentConsoleRunner
{
    public static async Task RunAsync(AIAgent agent, FoundryMemoryProvider? foundryMemory = null)
    {
        var session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (input is null || input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var response = await agent.RunAsync(input, session);
            await WriteResponseAndHandleApprovalsAsync(agent, session, response);
        }

        if (foundryMemory is not null)
        {
            // Foundry memory extraction runs as a background job on the service.
            // Wait for it here so a restarted process can immediately recall what was just said.
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
