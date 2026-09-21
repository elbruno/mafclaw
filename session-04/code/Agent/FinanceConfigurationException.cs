// Objective: report configuration failures without echoing configured values.
// A. Carry a safe, application-authored error message to each host.
namespace MafClaw.Session04;

public sealed class FinanceConfigurationException(string message) : Exception(message);
