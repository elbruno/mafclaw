// Objective: observe worker exceptions inside an ordinary successful tracking task.
// A. Record the observed execution state. B. Preserve only a successful answer. C. Use sanitized detail.
namespace MafClaw.Sample46;

internal sealed record AttemptResult(OutcomeState State, string? Result, string Detail);
