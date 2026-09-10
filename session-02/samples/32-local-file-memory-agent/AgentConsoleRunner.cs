// Session flow:
// A. Create one agent session for the conversation.
// B. Send prompts to the Harness agent and print its responses.
// C. Let /memory reveal the exact local JSON used by the demo.
// D. Ignore blank input and exit cleanly with /exit.

using Microsoft.Agents.AI;

internal static class AgentConsoleRunner
{
    public static async Task RunAsync(AIAgent agent, LocalFileMemoryStore memoryStore)
    {
        // Reuse one session for the live conversation; the JSON survives new sessions.
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

            if (input.Trim().Equals("/memory", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Local memory file: {memoryStore.MemoryPath}");
                Console.WriteLine(await memoryStore.ReadMemoryFileAsync());
                continue;
            }

            // Harness lets the model choose only from the two fixed-scope memory tools.
            var response = await agent.RunAsync(input, session);
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                Console.WriteLine(response.Text);
            }
        }
    }
}
