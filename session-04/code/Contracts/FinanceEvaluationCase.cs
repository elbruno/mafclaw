// Objective: describe one versioned synthetic behavior contract.
// A. Identify the check, its public input and expected observable result.
namespace MafClaw.Session04;

public sealed record FinanceEvaluationCase(string Id, string Kind, string Input, string Expected);
