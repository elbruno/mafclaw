// Objective: report deterministic evidence without storing prompts or credentials.
// A. Record the case identifier, outcome and safe reason.
namespace MafClaw.Session04;

public sealed record FinanceCheckResult(string Id, bool Passed, string Detail);
