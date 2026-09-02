using Azure;
using Azure.Identity;
using System.ClientModel;

namespace MafClaw.Session01;

/// <summary>
/// Maps known infrastructure exceptions to safe (message, exitCode) pairs.
/// This is the single, testable decision table for auth/service error dispatch.
/// Never exposes exception message text, URLs, tenant IDs, tokens, or identifiers.
/// </summary>
internal static class ErrorDispatch
{
    /// <summary>
    /// Maps a caught exception to a safe output message and exit code.
    /// Returns null for unrecognized exceptions — callers must let those propagate.
    /// </summary>
    public static (string message, int exitCode)? TryMap(Exception ex) => ex switch
    {
        CredentialUnavailableException  => (AuthErrorFormatter.CredentialUnavailable(), 3),
        AuthenticationFailedException   => (AuthErrorFormatter.AuthenticationFailed(),   3),
        ClientResultException cre       => (AuthErrorFormatter.RequestFailed(cre.Status), 4),
        RequestFailedException rfe      => (AuthErrorFormatter.RequestFailed(rfe.Status), 4),
        HttpRequestException            => (AuthErrorFormatter.NetworkFailure(),           5),
        _                               => null
    };
}
