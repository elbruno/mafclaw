// Objective: carry truthful typed facts that a model narrative cannot overwrite.
// A. Name each worker. B. Record attempts and elapsed report time. C. Include results only for success.
namespace MafClaw.Sample46;

public sealed record WorkerOutcome(
    string Worker, OutcomeState State, int Attempts, TimeSpan Elapsed, string? Result, string Detail);
