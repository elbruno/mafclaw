// Objective: keep orchestration evidence independent of model narrative.
// A. Report bounded-turn completion.
// B. Expose actual tool transcript entries for regression tests.
// C. Record terminal session cleanup and remaining running work.

using MafClaw.OrchestrationSupport;

namespace MafClaw.Sample43;

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
