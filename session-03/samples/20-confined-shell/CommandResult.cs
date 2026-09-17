// Objective: make the launched process outcome inspectable.
// Steps:
// A. Identify the sample-owned process and its exit code.
// B. Return both captured streams and the timeout status.
// C. Distinguish timed-out work from ordinary command completion.

namespace MafClaw.Sample20;

internal sealed record CommandResult(
    int ProcessId,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);
