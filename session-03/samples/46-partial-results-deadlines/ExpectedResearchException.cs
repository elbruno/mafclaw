// Objective: identify an expected permanent mock-data outage without treating it as success.
// A. Classify the outage. B. Keep its message safe. C. Do not opt it into retries.
namespace MafClaw.Sample46;

public sealed class ExpectedResearchException() : Exception("Expected fictional data source unavailable.");
