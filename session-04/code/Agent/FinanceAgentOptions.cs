// Objective: separate shared agent behavior from host-specific permissions.
// A. Select a host profile.
// B. Inject a real or explicitly scripted chat client.
// C. Choose supported local capabilities without enabling them in hosted mode.
using Azure.Core;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04;

public sealed class FinanceAgentOptions
{
    public FinanceHostProfile Profile { get; init; } = FinanceHostProfile.Local;
    public FinanceSettings? Settings { get; init; }
    public IChatClient? ChatClient { get; init; }
    public TokenCredential? Credential { get; init; }
    public TokenCredential? PurviewCredential { get; init; }
    public string WorkingDirectory { get; init; } = Path.Combine(AppContext.BaseDirectory, "working");
    public bool EnableShell { get; init; } = true;
    public bool EnableCodeAct { get; init; } = true;
    public bool EnableMemory { get; init; } = true;
    public bool EnableResearch { get; init; } = true;
}
