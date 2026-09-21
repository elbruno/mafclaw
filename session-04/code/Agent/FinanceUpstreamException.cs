// Objective: preserve an upstream failure without forwarding credentials or endpoints.
// A. Carry only an application-authored category and optional HTTP status.
namespace MafClaw.Session04;

public sealed class FinanceUpstreamException(string category, int? status = null)
    : Exception(status is null ? category : $"{category} (HTTP {status}).");
