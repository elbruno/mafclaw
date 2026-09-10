// Session flow:
// A. Detect presenter prompts that should become durable profile memory.
// B. Save those facts directly to the configured Foundry memory store.
// C. Count saved memories for the same scope so the demo has visible proof.

using Azure.AI.Projects;
using Azure.AI.Projects.Memory;

internal sealed class FoundryMemoryDemoStore(AIProjectClient projectClient, string memoryStoreName, string scope)
{
    public string Scope => scope;

    public static bool IsRememberPrompt(string input)
    {
        return input.TrimStart().StartsWith("Remember ", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SaveUserProfileMemoryAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        // Store the user's exact teaching prompt as a user-profile memory.
        await projectClient.MemoryStores.CreateMemoryAsync(
            memoryStoreName,
            scope,
            userMessage.Trim(),
            MemoryItemKind.UserProfile,
            cancellationToken);
    }

    public async Task<int> CountUserProfileMemoriesAsync(CancellationToken cancellationToken = default)
    {
        var count = 0;
        await foreach (var _ in projectClient.MemoryStores.GetMemoriesAsync(
            memoryStoreName,
            scope,
            MemoryItemKind.UserProfile,
            limit: 100,
            order: null,
            after: null,
            before: null,
            cancellationToken))
        {
            count++;
        }

        return count;
    }
}
