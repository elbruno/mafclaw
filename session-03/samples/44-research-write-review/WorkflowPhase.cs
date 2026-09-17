// Objective: name the host's legal workflow states.
// A. Require research before drafting.
// B. Allow at most one reviewed revision.
// C. Make completion and failure terminal.

namespace MafClaw.Sample44;

public enum WorkflowPhase
{
    Ready,
    Researched,
    Drafted,
    Reviewed,
    Revised,
    ReviewedRevision,
    Complete,
    Failed
}
