// Objective: represent fictional worker input, not market data.
// A. Keep a stable source reference.
// B. Keep the classroom observation.
// C. Provide no executable commands or output paths.

namespace MafClaw.Sample47;

public sealed record AnalysisObservation(string Reference, string Observation);
