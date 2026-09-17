// Objective: expose host evidence separately from the main agent's narrative.
// A. Preserve bounded-turn completion and cleanup status.
// B. Keep the actual tool transcript for regression assertions.
// C. Report remaining running tasks after releasing the SDK session.

using MafClaw.OrchestrationSupport;

namespace MafClaw.Sample42;

public sealed record TurnResult(
    string Narrative,
    bool Completed,
    bool SessionReleased,
    int RunningAfterRelease,
    IReadOnlyList<TranscriptEntry> Events)
{
    public bool ForegroundFinished { get; init; }
    public int ExitCode => Completed ? 0 : 1;
}
