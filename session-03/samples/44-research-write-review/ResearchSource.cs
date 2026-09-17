// Objective: represent one fictional, read-only research source.
// A. Carry its stable citation identifier.
// B. Carry the mock observation.
// C. Avoid representing it as real market information.

namespace MafClaw.Sample44;

public sealed record ResearchSource(string Id, string Observation);
