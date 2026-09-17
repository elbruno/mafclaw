// Objective: retain an observed event independently from assistant narration.
// Steps:
// A. Identify the agent and event state.
// B. Attach the timestamp and bounded, displayable evidence.

namespace MafClaw.OrchestrationSupport;

public sealed record TranscriptEntry(DateTimeOffset Timestamp, string Agent, string State, string Detail);
