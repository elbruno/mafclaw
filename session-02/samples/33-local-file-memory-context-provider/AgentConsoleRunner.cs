// Session flow:
// A. Create one agent session for the conversation.
// B. Send prompts to the Harness agent and print its responses.
// C. Let /memory reveal the exact files managed by FileMemoryProvider.
// D. Ignore blank input and exit cleanly with /exit.

using Microsoft.Agents.AI;

internal static class AgentConsoleRunner
{
    public static async Task RunAsync(AIAgent agent, string memoryDirectory)
    {
        // Reuse one session for the live conversation; the files survive new sessions.
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
                await PrintMemoryFilesAsync(memoryDirectory);
                continue;
            }

            // Harness lets the FileMemoryProvider add local memory tools and instructions.
            var response = await agent.RunAsync(input, session);
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                Console.WriteLine(response.Text);
            }
        }
    }

    private static async Task PrintMemoryFilesAsync(string memoryDirectory)
    {
        if (!Directory.Exists(memoryDirectory))
        {
            Console.WriteLine("No local memory files have been created yet.");
            return;
        }

        var files = Directory
            .EnumerateFiles(memoryDirectory, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (files.Length == 0)
        {
            Console.WriteLine("No local memory files have been created yet.");
            return;
        }

        foreach (var file in files)
        {
            Console.WriteLine($"--- {Path.GetRelativePath(memoryDirectory, file)} ---");
            Console.WriteLine(await File.ReadAllTextAsync(file));
        }
    }
}
