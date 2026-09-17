// Objective: allow retry only for an explicitly known host adapter failure.
// A. Declare the retry classification. B. Exclude arbitrary cloud exceptions. C. Avoid exposing error payloads.
namespace MafClaw.Sample46;

public sealed class RetryableResearchException() : Exception("Known transient fixture-adapter failure.");
