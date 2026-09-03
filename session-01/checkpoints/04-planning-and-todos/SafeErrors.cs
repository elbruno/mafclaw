using Azure;
using Azure.Identity;
using System.ClientModel;

namespace MafClaw.Checkpoint04;

internal static class SafeErrors
{
    public static int Write(Exception exception)
    {
        var (message, exitCode) = exception switch
        {
            FoundryConfigurationException configuration => (configuration.Message, 2),
            CredentialUnavailableException => (
                "Authentication unavailable. Run 'az login' for the tenant containing the Foundry project and retry.",
                3),
            AuthenticationFailedException => (
                "Authentication failed. Run 'az login' for the tenant containing the Foundry project and retry.",
                3),
            ClientResultException result => (
                $"Foundry request failed (HTTP {result.Status}). Check configuration and retry.",
                4),
            RequestFailedException request => (
                $"Foundry request failed (HTTP {request.Status}). Check configuration and retry.",
                4),
            HttpRequestException => (
                "Network error while contacting Foundry. Check connectivity and retry.",
                5),
            _ => (
                "The checkpoint could not complete. Check local configuration and retry.",
                6)
        };

        Console.Error.WriteLine(message);
        return exitCode;
    }
}
