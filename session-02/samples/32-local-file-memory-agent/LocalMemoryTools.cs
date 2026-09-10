// Session flow:
// A. Expose only current-user memory operations to the model.
// B. Save one durable profile fact through an explicit tool.
// C. Recall only the fixed current-user scope.
// D. Never accept a path or another user's identifier from the model.

using System.ComponentModel;
using Microsoft.Extensions.AI;

internal sealed class LocalMemoryTools
{
    private readonly LocalFileMemoryStore memoryStore;

    public LocalMemoryTools(LocalFileMemoryStore memoryStore)
    {
        this.memoryStore = memoryStore;
        RememberCurrentUserProfile = AIFunctionFactory.Create(
            RememberCurrentUserProfileAsync,
            "remember_current_user_profile");
        RecallCurrentUserProfile = AIFunctionFactory.Create(
            RecallCurrentUserProfileAsync,
            "recall_current_user_profile");
    }

    public AIFunction RememberCurrentUserProfile { get; }

    public AIFunction RecallCurrentUserProfile { get; }

    [Description("Saves one durable investing-profile fact for the current user only.")]
    private Task<string> RememberCurrentUserProfileAsync(
        [Description("A concise fact about the current user's investing profile, goal, time horizon, or preference.")] string fact,
        CancellationToken cancellationToken)
    {
        // The tool accepts a fact, but never a user ID or file path.
        return memoryStore.SaveProfileFactAsync(fact, cancellationToken);
    }

    [Description("Recalls all saved investing-profile facts for the current user only.")]
    private Task<string> RecallCurrentUserProfileAsync(CancellationToken cancellationToken)
    {
        // The fixed store scope prevents cross-user memory lookup.
        return memoryStore.RecallProfileAsync(cancellationToken);
    }
}
