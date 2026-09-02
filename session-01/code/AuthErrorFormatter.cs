namespace MafClaw.Session01;

/// <summary>
/// Produces safe, generic error messages for authentication and service failures.
/// Never includes exception message text, tenant IDs, account names, tokens,
/// endpoints, correlation IDs, or authentication-chain details.
/// </summary>
internal static class AuthErrorFormatter
{
    public static string CredentialUnavailable() =>
        "Authentication unavailable. Run 'az login' for the tenant containing the Foundry project and retry.";

    public static string AuthenticationFailed() =>
        "Authentication failed. Run 'az login' for the tenant containing the Foundry project and retry.";

    /// <summary>
    /// Safe request-failure message. Includes the HTTP status code (non-sensitive)
    /// but never includes the service error message, endpoint URL, or request identifiers.
    /// </summary>
    public static string RequestFailed(int statusCode) =>
        $"Live mode service request failed (HTTP {statusCode}). Check your Foundry configuration and retry.";

    public static string NetworkFailure() =>
        "Live mode network error. Check connectivity and retry.";
}
