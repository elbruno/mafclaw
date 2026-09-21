// Objective: teach a local application rule without pretending it is Purview.
// A. Recognize an explicitly synthetic restricted marker.
// B. Reject real transaction requests.
// C. Record only a bounded rule identifier, never the protected content.
namespace MafClaw.Session04;

public static class FinancePolicy
{
    public const string RestrictedMarker = "SYNTHETIC_RESTRICTED";
    public const string BlockMessage = "Blocked by the educational application policy. No protected action was executed.";

    public static string? GetBlockingRule(string text)
    {
        if (text.Contains(RestrictedMarker, StringComparison.OrdinalIgnoreCase))
            return "synthetic-restricted-marker";
        if (text.Contains("place a real trade", StringComparison.OrdinalIgnoreCase))
            return "no-real-transactions";
        return null;
    }
}
