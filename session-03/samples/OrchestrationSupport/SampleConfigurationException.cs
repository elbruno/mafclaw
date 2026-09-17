// Objective: distinguish an actionable, safe setup message from a raw cloud exception.
// Steps:
// A. Carry only a message authored by the sample host.
// B. Let the console report it without exposing configuration values.

namespace MafClaw.OrchestrationSupport;

public sealed class SampleConfigurationException(string message) : InvalidOperationException(message);
