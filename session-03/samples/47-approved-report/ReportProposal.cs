// Objective: preserve the exact bytes reviewed by the human.
// A. Store immutable report content.
// B. Bind SHA-256 and a host version to that content.
// C. Keep approval state outside model-visible proposal data.

namespace MafClaw.Sample47;

public sealed record ReportProposal(string Content, string Sha256, long Version);
