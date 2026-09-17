// Objective: compare the same evidence contract across independent runnable samples.
// A. Normalize the returned narrative and completion state.
// B. Preserve terminal provider cleanup evidence.
// C. Keep the real transcript rather than fabricate worker events.

using MafClaw.OrchestrationSupport;

namespace MafClaw.SpecialistSamples.Tests;

public sealed record ObservedTurn(
    string Narrative, bool Completed, bool SessionReleased, int RunningAfterRelease,
    IReadOnlyList<TranscriptEntry> Events)
{
    public bool ForegroundFinished { get; init; }
    public int ExitCode { get; init; }
}
